using Base.Repository.Common;
using Base.Repository.Identity;
using Base.Service.Common;
using Base.Service.IService;
using Base.Service.ViewModel.RequestVM;
using Base.Service.ViewModel.ResponseVM;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Duende.IdentityServer.Extensions;
using FirebaseAdmin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Text;
using Role = Base.Repository.Identity.Role;

namespace Base.Service.Service;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Cloudinary _cloudinary;
    private readonly ICurrentUserService _currentUserService;

    public UserService(UserManager<User> userManager, 
        IUnitOfWork unitOfWork, 
        Cloudinary cloudinary,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _cloudinary = cloudinary;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResponseVM<User>> CreateNewUser(UserVM newEntity)
    {
        var existedUser = await _userManager.FindByNameAsync(newEntity.UserName);
        if(existedUser is not null)
        {
            return new ServiceResponseVM<User>
            {
                IsSuccess = false,
                Title = "Create user failed",
                Errors = new string[1] { "Username is already taken" }
            };
        }

        if(newEntity.Email is not null)
        {
            var existedEmail = await _unitOfWork.UserRepository.Get(l => newEntity.Email.Equals(l.Email)).FirstOrDefaultAsync();
            if (existedEmail is not null)
            {
                return new ServiceResponseVM<User>
                {
                    IsSuccess = false,
                    Title = "Create user failed",
                    Errors = new string[1] { "Email is already taken" }
                };
            }
        }

        if (newEntity.PhoneNumber is not null)
        {
            var existedPhone = await _unitOfWork.UserRepository.Get(l => newEntity.PhoneNumber.Equals(l.PhoneNumber)).FirstOrDefaultAsync();
            if (existedPhone is not null)
            {
                return new ServiceResponseVM<User>
                {
                    IsSuccess = false,
                    Title = "Create user failed",
                    Errors = new string[1] { "Phone is already taken" }
                };
            }
        }

        User newUser = new User
        {
            DisplayName = newEntity.DisplayName,
            UserName = newEntity.UserName,
            PhoneNumber = newEntity.PhoneNumber,
            Email = newEntity.Email,
            LockoutEnabled = newEntity.LockoutEnabled ?? false,
            LockoutEnd = newEntity.LockoutEnd,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.UserId
        };

        if(newEntity.RoleIds.Count() > 0)
        {
            List<Role> roles = new List<Role>();
            foreach (Guid id in newEntity.RoleIds)
            {
                var existedRole = await _unitOfWork.RoleRepository.FindAsync(id);
                if (existedRole is null)
                {
                    return new ServiceResponseVM<User>
                    {
                        IsSuccess = false,
                        Title = "Create user failed",
                        Errors = new List<string>() { $"Role with id:{id} not found" }
                    };
                }
                else
                {
                    roles.Add(existedRole);
                }
            }
            if (roles.Count() > 0)
            {
                newUser.Roles = roles;
            }
        }

        //Upload file
        var file = newEntity.Avatar;
        if (file is not null && file.Length > 0)
        {
            var uploadFile = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream())
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadFile);

            if (uploadResult.Error is not null)
            {
                // Log error here
            }
            else
            {
                newUser.FilePath = uploadResult.SecureUrl.ToString();
            }
        }
        else if (newEntity.FilePath is not null)
        {
            newUser.FilePath = newEntity.FilePath;
        }

        try
        {
            var identityResult = await _userManager.CreateAsync(newUser, newEntity.Password);
            if (!identityResult.Succeeded)
            {
                return new ServiceResponseVM<User>
                {
                    IsSuccess = false,
                    Title = "Create user failed",
                    Errors = identityResult.Errors.Select(e => e.Description)
                };
            }
            else
            {
                return new ServiceResponseVM<User>
                {
                    Result = newUser,
                    IsSuccess = true,
                    Title = "Create user successfully"
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<User>
            {
                IsSuccess = false,
                Title = "Create user failed",
                Errors = new List<string>() { ex.Message }
            };
        }
        catch (OperationCanceledException)
        {
            return new ServiceResponseVM<User>
            {
                IsSuccess = false,
                Title = "Create user failed",
                Errors = new string[1] { "The operation has been cancelled" }
            };
        }
    }

    public async Task<User?> GetUserById(Guid id)
    {
        var include = new Expression<Func<User, object>>[]
        {
            u => u.Roles
        };
        return await _unitOfWork
            .UserRepository
            .Get(u => !u.Deleted && u.Id == id, include)
            .FirstOrDefaultAsync();
    }

    public async Task<LoginUserManagement> LoginUser(LoginUserVM resource)
    {
        var existedUser = await _userManager.FindByNameAsync(resource.UserName);
        if(existedUser is null || existedUser.Deleted)
        {
            return new LoginUserManagement
            {
                IsSuccess = false,
                Title = "Login failed",
                Errors = new string[1] { "Invalid username or password" }
            };
        }
        var checkPassword = await _userManager.CheckPasswordAsync(existedUser, resource.Password);
        if (!checkPassword)
        {
            return new LoginUserManagement
            {
                IsSuccess = false,
                Title = "Login failed",
                Errors = new string[1] { "Invalid username or password" }
            };
        }

        if (existedUser.LockoutEnabled)
        {
            return new LoginUserManagement
            {
                IsSuccess = false,
                Title = "Login failed",
                Errors = new string[1] { "Account is blocked" }
            };
        }
        else
        {
            var roles = await _unitOfWork.RoleRepository.Get(r => !r.Deleted && r.Users.Contains(existedUser)).ToArrayAsync();
            existedUser.Roles = roles;
            return new LoginUserManagement
            {
                Title = "Login Successfully",
                IsSuccess = true,
                LoginUser = existedUser,
                RoleNames = roles.Select(r => r.Name)
            };
        }

    }

    public async Task<LoginUserManagement> LoginWithGoogle(string idToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(idToken);
        var claims = jwtSecurityToken.Claims;

        if (claims.IsNullOrEmpty())
        {
            return new LoginUserManagement
            {
                IsSuccess = false,
                Title = "Login failed",
                Errors = new string[2] { "Invalid id token", "Claims not found" }
            };
        }

        var email = claims.Where(c => c.Type == "email").FirstOrDefault()?.Value;
        if(email is null)
        {
            return new LoginUserManagement
            {
                IsSuccess = false,
                Title = "Login failed",
                Errors = new string[1] { "Invalid email or email not found" }
            };
        }

        var existedUser = await _userManager.FindByEmailAsync(email);
        if(existedUser is not null)
        {
            if (existedUser.LockoutEnabled)
            {
                return new LoginUserManagement
                {
                    IsSuccess = false,
                    Title = "Login failed",
                    Errors = new string[1] { "Account is blocked" }
                };
            }

            var roles = await _unitOfWork.RoleRepository.Get(r => !r.Deleted && r.Users.Contains(existedUser)).ToArrayAsync();
            existedUser.Roles = roles;
            return new LoginUserManagement
            {
                IsSuccess = true,
                Title = "Login Successfully",
                LoginUser = existedUser,
                RoleNames = roles.Select(r => r.Name)
            };
        }

        User newUser = new User
        {
            UserName = claims.FirstOrDefault(c => c.Type == "email")?.Value,
            DisplayName = claims.FirstOrDefault(c => c.Type == "name")?.Value,
            Email = claims.FirstOrDefault(c => c.Type == "email")?.Value,
            LockoutEnabled = false,
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUserService.UserId,
            FilePath = claims.FirstOrDefault(c => c.Type == "picture")?.Value
        };

        try
        {
            var result = await _userManager.CreateAsync(newUser);
            if (!result.Errors.Any())
            {
                return new LoginUserManagement
                {
                    Title = "Login Successfully",
                    IsSuccess = true,
                    LoginUser = newUser
                };
            }
            else
            {
                return new LoginUserManagement
                {
                    IsSuccess = false,
                    Title = "Login failed",
                    Errors = result.Errors.Select(e => e.Description)
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new LoginUserManagement
            {
                IsSuccess = false,
                Title = "Login failed",
                Errors = new string[2] { ex.Message, "Create user failed" }
            };
        }
        catch (OperationCanceledException)
        {
            return new LoginUserManagement
            {
                IsSuccess = false,
                Title = "Login failed",
                Errors = new string[2] { "The operation has been cancelled", "Create user failed" }
            };
        }
    }

    public async Task<ServiceResponseVM<User>> UpdateUser(UserVM resource, Guid id)
    {
        var existedUser = await _unitOfWork
            .UserRepository
            .Get(u => !u.Deleted && u.Id == id)
            .FirstOrDefaultAsync();

        if(existedUser == null)
        {
            return new ServiceResponseVM<User>
            {
                IsSuccess = false,
                Title = "Update user failed",
                Errors = new string[1] { "User not found" }
            };
        }

        if(resource.Email is not null)
        {
            var existedEmail = await _unitOfWork.UserRepository.Get(u => u.Email.Equals(resource.Email)).AsNoTracking().FirstOrDefaultAsync();
            if (existedEmail is not null)
            {
                return new ServiceResponseVM<User>
                {
                    IsSuccess = false,
                    Title = "Update user failed",
                    Errors = new string[1] { "Email is already taken" }
                };
            }
            existedUser.Email = resource.Email;
            existedUser.NormalizedEmail = resource.Email.ToUpper();
        }

        if(resource.PhoneNumber is not null)
        {
            var existedPhone = await _unitOfWork.UserRepository.Get(u => u.PhoneNumber.Equals(resource.PhoneNumber)).AsNoTracking().FirstOrDefaultAsync();
            if (existedPhone is not null)
            {
                return new ServiceResponseVM<User>
                {
                    IsSuccess = false,
                    Title = "Update user failed",
                    Errors = new string[1] { "Phone is already taken" }
                };
            }
            existedUser.PhoneNumber = resource.PhoneNumber;
        }

        existedUser.LockoutEnd = resource.LockoutEnd ?? existedUser.LockoutEnd;
        existedUser.LockoutEnabled = resource.LockoutEnabled ?? existedUser.LockoutEnabled;
        existedUser.DisplayName = resource.DisplayName ?? existedUser.DisplayName;

        //Upload file
        var file = resource.Avatar;
        if (file is not null && file.Length > 0)
        {
            var uploadFile = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, file.OpenReadStream())
            };
            var uploadResult = await _cloudinary.UploadAsync(uploadFile);

            if (uploadResult.Error is not null)
            {
                // Log error here
            }
            else
            {
                existedUser.FilePath = uploadResult.SecureUrl.ToString();
            }
        }
        else if (resource.FilePath is not null)
        {
            existedUser.FilePath = resource.FilePath;
        }

        try
        {
            var updateResult = await _unitOfWork.SaveChangesAsync();
            if (updateResult)
            {
                return new ServiceResponseVM<User>
                {
                    IsSuccess = true,
                    Title = "Update user successfully"
                };
            }
            else
            {
                return new ServiceResponseVM<User>
                {
                    IsSuccess = false,
                    Title = "Update user failed"
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<User>
            {
                IsSuccess = false,
                Title = "Update user failed",
                Errors = new string[1] { ex.Message }
            };
        }
        catch (OperationCanceledException)
        {
            return new ServiceResponseVM<User>
            {
                IsSuccess = false,
                Title = "Update user failed",
                Errors = new string[1] { "The operation has been cancelled" }
            };
        }
    }

    public async Task<string?> GetCode()
    {
        var userId = _currentUserService.UserId;
        if(userId is null)
        {
            return null;
        }

        var existedUser = await _unitOfWork.UserRepository.FindAsync(new Guid(userId));
        if(existedUser is null)
        {
            return null;
        }

        // Generate new Code
        int length = 7;
        StringBuilder str_build = new StringBuilder();
        Random random = new Random();
        char letter;
        for (int i = 0; i < length; i++)
        {
            double flt = random.NextDouble();
            int shift = Convert.ToInt32(Math.Floor(25 * flt));
            letter = Convert.ToChar(shift + 65);
            str_build.Append(letter);
        }
        var code = str_build.ToString();

        if(code is null)
        {
            return null;
        }

        existedUser.Code = code;
        var result = await _unitOfWork.SaveChangesAsync();
        if (!result)
        {
            return null;
        }

        return code;
    }
}
