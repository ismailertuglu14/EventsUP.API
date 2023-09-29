using MassTransit;
using Topluluk.Services.PostAPI.Data.Interface;
using Topluluk.Shared.Dtos;
using Topluluk.Shared.Messages.User;

namespace Topluluk.Services.PostAPI.Services.Consumer
{
    public class UserUpdatedConsumer : IConsumer<UserUpdatedEvent>
    {
        private readonly IPostRepository _postRepository;
        private readonly IPostCommentRepository _postCommentRepository;
        private readonly IPostInteractionRepository _postInteractionRepository;
        private readonly ISavedPostRepository _savedPostRepository;
        private readonly ICommentInteractionRepository _commentInteractionRepository;

        public UserUpdatedConsumer(
            IPostRepository postRepository, IPostCommentRepository postCommentRepository, IPostInteractionRepository postInteractionRepository,
            ISavedPostRepository savedPostRepository, ICommentInteractionRepository commentInteractionRepository
            )
        {
            _postRepository = postRepository;
            _postCommentRepository = postCommentRepository;
            _postInteractionRepository = postInteractionRepository;
            _savedPostRepository = savedPostRepository;
            _commentInteractionRepository = commentInteractionRepository;
        }

        public async Task Consume(ConsumeContext<UserUpdatedEvent> context)
        {
            User user = new()
            {
                Id = context.Message.Id,
                FullName = context.Message.FullName,
                UserName = context.Message.UserName,
                Gender = context.Message.Gender,
                ProfileImage = context.Message.ProfileImage,
            };

            var postupdateResult = await _postRepository.UserUpdated(user);
            var commentUpdateResult = await _postCommentRepository.UserUpdated(user);
            var savedPostUpdateResult = "";
            var postInteractionUpdateResult = "";
            var commentInteractionRepository = "";
            if (postupdateResult == false) return;
            
        }
    }
}
