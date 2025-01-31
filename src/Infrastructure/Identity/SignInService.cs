using Hippo.Application.Common.Interfaces;
using Hippo.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Hippo.Infrastructure.Identity;

public class SignInService : ISignInService
{
    private readonly SignInManager<Account> _signInManager;

    private readonly UserManager<Account> _userManager;

    public SignInService(SignInManager<Account> signInManager, UserManager<Account> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<Result> PasswordSignInAsync(string username, string password, bool rememberMe = false)
    {
        var user = _userManager.Users.SingleOrDefault(u => u.UserName == username);

        if (user == null)
        {
            return Result.Failure(new string[] { "Account does not exist." });
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, false);

        return result.ToApplicationResult();
    }

    public async Task<Unit> SignOutAsync()
    {
        await _signInManager.SignOutAsync();
        return Unit.Value;
    }
}
