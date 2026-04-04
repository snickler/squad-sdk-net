using Squad.SDK.NET.Config;
using Squad.SDK.NET.Hooks;

namespace Squad.SDK.NET.Builder;

public sealed class CeremonyBuilder
{
    private string? _name;
    private string? _trigger;
    private string? _schedule;
    private readonly List<string> _participants = [];
    private readonly List<string> _agenda = [];
    private PolicyConfig? _hooks;

    public CeremonyBuilder Name(string name) { _name = name; return this; }
    public CeremonyBuilder Trigger(string trigger) { _trigger = trigger; return this; }
    public CeremonyBuilder Schedule(string schedule) { _schedule = schedule; return this; }
    public CeremonyBuilder Participants(params string[] participants) { _participants.AddRange(participants); return this; }
    public CeremonyBuilder Agenda(params string[] items) { _agenda.AddRange(items); return this; }
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
