namespace CrawfisSoftware.Events
{
    /// <summary>
    /// Raw input requests: the one-way funnel every input source publishes into. Publishing is
    /// open — the <c>Scripts/Input/</c> action classes, <c>AIController</c>, and any future replay
    /// or netcode driver — while subscribing is closed to <c>Input2TempleRunAutoEventBridge</c>
    /// alone. Gameplay controllers subscribe to the TempleRun event the bridge translates into.
    ///
    /// <para><b>Every member carries the player id, and nothing else.</b> It is the one fact an
    /// input request always has and a handler can never recover on its own, so the funnel is
    /// uniform: no member is payload-free, none carries anything but the id. Three payload types
    /// used to share these nine members — most carried the id, two carried
    /// <c>UnityEngine.Time.time</c>, and <c>AIController</c> put the run distance on the same two
    /// turn events the input classes put the id on. None of the three was read, and none told a
    /// subscriber <i>who</i> asked. The clock is deliberately not carried: any handler can read
    /// it, so putting it on the message only obscures what the message is for.</para>
    ///
    /// <para>The template is single-player and every source publishes <c>0</c>. The declaration is
    /// what makes a second player a wiring change rather than a payload redesign.</para>
    /// </summary>
    [EventEnum]
    public enum UserInitiatedEvents
    {
        [EventPayload(typeof(int))]  // Player id
        UserLeftTurnRequested,
        [EventPayload(typeof(int))]  // Player id
        UserRightTurnRequested,
        [EventPayload(typeof(int))]  // Player id
        UserPauseToggle,
        [EventPayload(typeof(int))]  // Player id
        UserLeftLaneChangeRequested,
        [EventPayload(typeof(int))]  // Player id
        UserRightLaneChangeRequested,
        [EventPayload(typeof(int))]  // Player id
        UserJumpRequested,
        [EventPayload(typeof(int))]  // Player id
        UserQuitRequested,
        [EventPayload(typeof(int))]  // Player id
        UserSlideRequested,
        [EventPayload(typeof(int))]  // Player id
        UserDashRequested,
    }
}
