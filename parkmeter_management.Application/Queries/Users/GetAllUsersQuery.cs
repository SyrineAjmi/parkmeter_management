using MediatR;
using parkmeter_management.Domain.Entities;

namespace parkmeter_management.Application.Queries.Users
{
    public class GetAllUsersQuery : IRequest<List<User>> { }
}