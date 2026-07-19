using UnityEngine;

namespace CrawfisSoftware.Events
{
    [DefaultExecutionOrder(-10000)]
    /// <summary>
    /// Singleton event publisher that interfaces to the CrawfisSoftware.Events EventsPublisher framework.
    /// Avoids the problem with strings and misspelling when dealing with the EventsPublisher. Several of these could be used with
    /// different enum types for more modularity.
    /// </summary>

    class EventsPublisherUserInitiated : EventsPublisherEnumsSingleton<UserInitiatedEvents>
    {
    }
}