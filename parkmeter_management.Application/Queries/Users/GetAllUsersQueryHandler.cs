using MediatR;
using parkmeter_management.Application.Interfaces.Repositories;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Users
{
    public class GetAllUsersQueryHandler
        : IRequestHandler<GetAllUsersQuery, List<User>>
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<User>> Handle(
            GetAllUsersQuery request, CancellationToken ct)
        {
            var users = await _userRepository.GetAllAsync();
            return users.ToList();
        }
    }
}

