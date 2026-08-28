using System;
using System.Collections.Generic;

using UnityEngine;

namespace CrawfisSoftware.Events
{
    /// <summary>
    /// Publishes a declared set of follow-on events whenever a source event fires. This is the
    /// single implementation of the subscribe-to-all-then-dispatch logic that the auto-flow and
    /// bridge components used to each re-implement.
    /// </summary>
    /// <remarks>
    /// <para><b>Fan-out.</b> Chains are declared as a flat list of pairs rather than a dictionary,
    /// so one source event may declare several consequences. The dictionary this replaced allowed
    /// exactly one successor per event, and that ceiling did not produce bugs directly - it
    /// produced workarounds. A developer who found a source event's slot already taken published
    /// the second consequence by hand inside a controller instead, which is how failure logic came
    /// to publish pause events.</para>
    /// <para><b>Ordering.</b> Targets fire in declaration order, and publishing is synchronous, so
    /// a target's own chain completes before the next target here is published. Declaration order
    /// is therefore load-bearing when one source has several targets - keep such groups together
    /// and comment why the order matters.</para>
    /// <para><b>Validation gates.</b> Chaining runs before any controller validates, so a
    /// <c>*Requested</c> event that arrives raw from input must never be chained to its
    /// <c>*Starting</c>. The controller that validates publishes <c>*Starting</c> itself. Fan-out
    /// makes chaining easier to reach for; it does not make that any safer.</para>
    /// </remarks>
    /// <typeparam name="TSource">Enum family listened to.</typeparam>
    /// <typeparam name="TDest">Enum family published to. Same as <typeparamref name="TSource"/> for
    /// a same-domain auto-flow; different for a bridge.</typeparam>
    public sealed class EventChainDispatcher<TSource, TDest>
        where TSource : Enum
        where TDest : Enum
    {
        private readonly Dictionary<TSource, TDest[]> _chains;
        private readonly Action<string, object, object> _handler;
        private bool _attached;

        public EventChainDispatcher(IReadOnlyList<(TSource From, TDest To)> chains)
        {
            _chains = Group(chains);
            // Cached so Attach and Detach pass the same delegate instance.
            _handler = OnSourceEvent;
        }

        /// <summary>Number of source events that have at least one target. Diagnostics only.</summary>
        public int SourceCount => _chains.Count;

        public void Attach()
        {
            if (_attached) return;
            EventsFor<TSource>.SubscribeToAll(_handler);
            _attached = true;
        }

        public void Detach()
        {
            if (!_attached) return;
            EventsFor<TSource>.UnsubscribeFromAll(_handler);
            _attached = false;
        }

        private void OnSourceEvent(string eventName, object sender, object data)
        {
            if (!EventsFor<TSource>.TryGetEnum(eventName, out TSource sourceEvent)) return;
            if (!_chains.TryGetValue(sourceEvent, out TDest[] targets)) return;

            // The original sender is forwarded rather than replaced with the dispatcher, so an
            // event log still names whatever actually caused the chain.
            for (int i = 0; i < targets.Length; i++)
            {
                EventsFor<TDest>.Publish(targets[i], sender, data);
            }
        }

        private static Dictionary<TSource, TDest[]> Group(IReadOnlyList<(TSource From, TDest To)> chains)
        {
            if (chains == null) return new Dictionary<TSource, TDest[]>();

            var grouped = new Dictionary<TSource, List<TDest>>();
            for (int i = 0; i < chains.Count; i++)
            {
                (TSource from, TDest to) = chains[i];
                if (!grouped.TryGetValue(from, out List<TDest> targets))
                {
                    targets = new List<TDest>(1);
                    grouped[from] = targets;
                }
                else if (targets.Contains(to))
                {
                    // An exact duplicate pair would publish the same event twice from one source.
                    // Always a mistake, and silent without this.
                    Debug.LogWarning(
                        $"EventChainDispatcher: duplicate chain {from} -> {to} declared twice; ignoring the repeat.");
                    continue;
                }
                targets.Add(to);
            }

            var result = new Dictionary<TSource, TDest[]>(grouped.Count);
            foreach (KeyValuePair<TSource, List<TDest>> pair in grouped)
            {
                result[pair.Key] = pair.Value.ToArray();
            }
            return result;
        }
    }

    /// <summary>
    /// Base for a component that declares one direction of automatic event chaining. Subclasses
    /// supply <see cref="Chains"/> and nothing else; subscribe, dispatch and unsubscribe are
    /// handled here.
    /// </summary>
    /// <remarks>A component needing two directions (a bidirectional bridge) cannot inherit twice -
    /// it holds two <see cref="EventChainDispatcher{TSource,TDest}"/> instances instead.</remarks>
    public abstract class AutoEventFlowBase<TSource, TDest> : MonoBehaviour
        where TSource : Enum
        where TDest : Enum
    {
        private EventChainDispatcher<TSource, TDest> _dispatcher;

        /// <summary>
        /// Every (source -&gt; target) pair this component declares. Return a <c>static readonly</c>
        /// array; this is read once, in <c>Awake</c>.
        /// </summary>
        protected abstract IReadOnlyList<(TSource From, TDest To)> Chains { get; }

        protected virtual void Awake()
        {
            _dispatcher = new EventChainDispatcher<TSource, TDest>(Chains);
            _dispatcher.Attach();
        }

        protected virtual void OnDestroy()
        {
            _dispatcher?.Detach();
        }
    }
}
