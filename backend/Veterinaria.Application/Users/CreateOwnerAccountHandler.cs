using Veterinaria.Application.Abstractions;
using Veterinaria.Domain.Entities;
using Veterinaria.Domain.Enumerations;
using Veterinaria.Domain.Rules;

namespace Veterinaria.Application.Users;

public sealed class CreateOwnerAccountHandler(IUserRepository userRepository)
{
    public async Task<User> HandleAsync(CreateOwnerAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await userRepository.IdentificationExistsAsync(command.Identification, cancellationToken))
        {
            throw new DomainRuleException("RF-REG-06", "La identificación ya está registrada.");
        }

        if (await userRepository.EmailExistsAsync(command.Email, cancellationToken))
        {
            throw new DomainRuleException("RF-REG-06", "El correo ya está registrado.");
        }


        var user = new User(
            Guid.NewGuid(),
            command.Identification,
            command.Name,
            command.Email,
            command.Phone,
            UserRole.Owner);


        await userRepository.CreateAsync(user, command.Password, cancellationToken);

        return user;
    }
}
