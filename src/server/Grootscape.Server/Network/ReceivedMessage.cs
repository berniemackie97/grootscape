namespace Grootscape.Server.Network;

internal readonly record struct ReceivedMessage(ReceivedKind Kind, int Length);

