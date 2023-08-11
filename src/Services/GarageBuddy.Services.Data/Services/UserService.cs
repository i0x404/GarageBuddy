﻿namespace GarageBuddy.Services.Data.Services
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Common.Core.Wrapper;

    using Contracts;

    using GarageBuddy.Common.Core.Wrapper.Generic;
    using GarageBuddy.Data.Models;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;

    using static Common.Constants.ErrorMessageConstants;
    using static Common.Constants.GlobalConstants;

    public class UserService : IUserService
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ILogger<UserService> logger;

        public UserService(SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager, ILogger<UserService> logger)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task<SignInResult> LoginWithUsernameAsync(string username, string password,
            bool isPersistent, bool lockoutOnFailure)
        {
            if (string.IsNullOrWhiteSpace(username.Trim()))
            {
                throw new ArgumentException(String.Format(ErrorCannotBeNullOrWhitespace, "Username"), nameof(username));
            }

            if (string.IsNullOrWhiteSpace(password.Trim()))
            {
                throw new ArgumentException(String.Format(ErrorCannotBeNullOrWhitespace, "Password"), nameof(password));
            }

            var user = await this.userManager.FindByNameAsync(username);
            var result = await this.signInManager.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
            return result;
        }

        public async Task<SignInResult> LoginWithEmailAsync(string email, string password, bool isPersistent,
            bool lockoutOnFailure)
        {
            if (string.IsNullOrWhiteSpace(email.Trim()))
            {
                throw new ArgumentException(String.Format(ErrorCannotBeNullOrWhitespace, "Email"), nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password.Trim()))
            {
                throw new ArgumentException(String.Format(ErrorCannotBeNullOrWhitespace, "Password"), nameof(password));
            }

            var user = await this.userManager.FindByEmailAsync(email);
            var result = await this.signInManager.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);

            return result;
        }

        public async Task<IdentityResult> RegisterWithEmailAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email.Trim()))
            {
                throw new ArgumentException(string.Format(ErrorCannotBeNullOrWhitespace, "Email"), nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password.Trim()))
            {
                throw new ArgumentException(string.Format(ErrorCannotBeNullOrWhitespace, "Password"), nameof(password));
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
            };

            var result = await this.userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await this.signInManager.SignInAsync(user, isPersistent: false);
            }

            // If it is the first user, make him admin
            if (this.userManager.Users.Count() == 1)
            {
                await this.userManager.AddToRoleAsync(user, AdministratorRoleName);
            }

            await signInManager.SignInAsync(user, false);

            return result;
        }

        public async Task LogoutAsync()
        {
            await signInManager.SignOutAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            var user = await this.userManager.FindByIdAsync(id.ToString());
            return user != null;
        }

        public async Task<IResult<string>> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                // Don't return more detailed information here to prevent email enumeration
                return await Result<string>.FailAsync(ErrorGeneral);
            }

            if (userManager.Options.SignIn.RequireConfirmedEmail && !(await userManager.IsEmailConfirmedAsync(user)))
            {
                return await Result<string>.FailAsync(ErrorGeneral);
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            return await Result<string>.SuccessAsync(data: token);
        }

        public async Task<IResult<string>> GenerateEmailResetUriAsync(string email, string origin, string route, string tokenQueryKey)
        {
            var tokenResult = await GeneratePasswordResetTokenAsync(email);
            if (!tokenResult.Succeeded)
            {
                return await Result<string>.FailAsync(tokenResult.Messages);
            }

            if (string.IsNullOrWhiteSpace(origin))
            {
                logger.LogError(ErrorCannotBeNullOrWhitespace, nameof(origin));
                return await Result<string>.FailAsync(ErrorGeneral);
            }

            if (string.IsNullOrWhiteSpace(route))
            {
                logger.LogError(ErrorCannotBeNullOrWhitespace, nameof(route));
                return await Result<string>.FailAsync(ErrorGeneral);
            }

            if (string.IsNullOrWhiteSpace(tokenQueryKey))
            {
                logger.LogError(ErrorCannotBeNullOrWhitespace, nameof(tokenQueryKey));
                return await Result<string>.FailAsync(ErrorGeneral);
            }

            var endpointUri = new Uri(string.Concat($"{origin}/", route));
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(tokenResult.Data));
            var verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), tokenQueryKey, token);
            return await Result<string>.SuccessAsync(verificationUri);
        }
    }
}
