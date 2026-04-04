using Squad.SDK.NET.Config;
using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Builder;

/// <summary>
/// Fluent builder for configuring a <see cref="CeremonyConfig"/>.
/// </summary>
/// <seealso cref="SquadBuilder"/>
public sealed class CeremonyBuilder
{
    private string? _name;
    private string? _trigger;
    private string? _schedule;
    private readonly List<string> _participants = [];
    private readonly List<string> _agenda = [];
    private PolicyConfig? _hooks;

    /// <summary>Sets the ceremony name.</summary>
    /// <param name="name">The ceremony name (e.g., <c>"standup"</c>).</param>
    /// <returns>This builder instance for chaining.</returns>
    public CeremonyBuilder Name(string name) { _name = name; return this; }

    /// <summary>Sets the event trigger for this ceremony.</summary>
    /// <param name="trigger">The trigger expression.</param>
    /// <returns>This builder instance for chaining.</returns>
    public CeremonyBuilder Trigger(string trigger) { _trigger = trigger; return this; }

    /// <summary>Sets a cron-like schedule for this ceremony.</summary>
    /// <param name="schedule">The schedule expression.</param>
    /// <returns>This builder instance for chaining.</returns>
    public CeremonyBuilder Schedule(string schedule) { _schedule = schedule; return this; }

    /// <summary>Adds participants (agent names) to the ceremony.</summary>
    /// <param name="participants">Agent names that participate.</param>
    /// <returns>This builder instance for chaining.</returns>
    public CeremonyBuilder Participants(params string[] participants) { _participants.AddRange(participants); return this; }

    /// <summary>Adds agenda items to the ceremony.</summary>
    /// <param name="items">Agenda item descriptions.</param>
    /// <returns>This builder instance for chaining.</returns>
    public CeremonyBuilder Agenda(params string[] items) { _agenda.AddRange(items); return this; }

    /// <summary>Sets hook policies for this ceremony.</summary>
    /// <param name="hooks">The <see cref="PolicyConfig"/> to apply.</param>
    /// <returns>This builder instance for chaining.</returns>
    public CeremonyBuilder Hooks(PolicyConfig hooks) { _hooks = hooks; return this; }

    internal CeremonyConfig Build()
    {
        if (string.IsNullOrWhiteSpace(_name))
            throw new InvalidOperationException("Ceremony name is required.");

        return new CeremonyConfig
        {
            Name = _name,
            Trigger = _trigger,
            Schedule = _schedule,
            Participants = _participants.AsReadOnly(),
            Agenda = _agenda.AsReadOnly(),
            Hooks = _hooks
        };
    }
}
