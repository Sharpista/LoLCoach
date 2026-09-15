namespace LoLCoach.Api.Infrastructure;

public sealed class AiCoachUnavailableException(string message, Exception? innerException = null) : Exception(message, innerException);
