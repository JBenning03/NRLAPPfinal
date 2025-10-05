namespace NRLApp.Models;

public class ErrorViewModel
{
// En tekstverdi som holder forespørselens ID (kan være null)
    public string? RequestId { get; set; }
// Returnerer true hvis RequestId IKKE er null eller tom
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
