namespace MiniInstagram.Models;

public record IncomingCallDto(
    string CallId,
    string CallerId,
    string CallerUserName,
    string CallerDisplayName,
    bool IsVideoCall);

public record CallEndedDto(string CallId, string Reason);
