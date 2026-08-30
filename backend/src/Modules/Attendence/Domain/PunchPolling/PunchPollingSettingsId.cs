public readonly record struct PunchPollingSettingsId(Guid Value)
{
    public static PunchPollingSettingsId New() => new(Guid.CreateVersion7());

    public static PunchPollingSettingsId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PunchPollingSettings id cannot be empty.", nameof(value));

        return new(value);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PunchPollingSettingsId id) => id.Value;

    public static explicit operator PunchPollingSettingsId(Guid value) => From(value);
}
