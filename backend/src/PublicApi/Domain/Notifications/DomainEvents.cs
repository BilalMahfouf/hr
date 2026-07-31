using Modules.Shared.Domain.Common;

namespace PublicApi.Domain.Notifications;

/// <summary>
/// Domain event raised by the daily reminder job when a vaccination's next-due date
/// falls within the next 24 hours.
/// </summary>
/// <param name="VaccinationId">The identifier of the vaccination that is due soon.</param>
public sealed record VaccinationDueDateReminderDomainEvent(Guid VaccinationId) : DomainEvent();

/// <summary>
/// Domain event raised by the daily reminder job when a confirmed or rescheduled
/// appointment is scheduled within the next 24 hours.
/// </summary>
/// <param name="AppointmentId">The identifier of the upcoming appointment.</param>
public sealed record UpcomingAppointmentReminderDomainEvent(Guid AppointmentId) : DomainEvent();

/// <summary>
/// Domain event raised by the daily reminder job once per active user at the start
/// of each working day, delivering a motivational prompt to use the system.
/// </summary>
public sealed record MotivationalDailyReminderDomainEvent() : DomainEvent();
