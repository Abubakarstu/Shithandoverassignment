namespace ShiftHandover.Models.Enums;

public enum DeliveryStatus
{
    /// <summary>Handed over to the SMTP server successfully.</summary>
    Sent = 0,

    /// <summary>SMTP was unavailable - the message was written to the local outbox instead.</summary>
    QueuedToOutbox = 1,

    Failed = 2
}