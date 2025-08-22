using CrawfisSoftware.Events;

namespace CrawfisSoftware.TempleRun
{
    /// <summary>
    /// Singleton event publisher that interfaces to the CrawfisSoftware.AssetManagement.EventsPublisher singleton.
    /// Avoids the problem with strings and misspelling when dealing with the EventsPublisher. Several of these could be used with
    /// different enum types for more modularity.
    /// </summary>
    public class EventsPublisherTempleRun : EventsPublisherEnumsSingleton<KnownEvents>
    {
    }
}