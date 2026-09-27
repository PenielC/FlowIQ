using Mediator;

namespace FlowIQ.Application.Feedback.Commands.SendFeedback;

public record SendFeedbackCommand(Guid UserId, string Message) : ICommand;
