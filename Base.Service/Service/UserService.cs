using Base.Repository.Common;
using Base.Repository.Identity;
using Base.Service.Common;
using Base.Service.IService;
using Base.Service.ViewModel.RequestVM;
using Base.Service.ViewModel.ResponseVM;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
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
                Title = "Username is already taken"
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
                    Title = "Email is already taken",
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
                    Title = "Phone is already taken"
                };
            }
        }

        User newUser = new User
        {
            DisplayName = newEntity.DisplayName,
            UserName = newEntity.UserName,
            PhoneNumber = newEntity.PhoneNumber,
            Email = newEntity.Email,
            LockoutEnabled = newEntity.LockoutEnabled ?? true,
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
                        Title = "Failed to register",
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
                    Title = "Failed to register",
                    Errors = identityResult.Errors.Select(e => e.Description)
                };
            }
            else
            {
                return new ServiceResponseVM<User>
                {
                    Result = newUser,
                    IsSuccess = true,
                    Title = "Register successfully"
                };
            }
        }
        catch (DbUpdateException ex)
        {
            return new ServiceResponseVM<User>
            {
                IsSuccess = false,
                Title = "Failed to register",
                Errors = new List<string>() { ex.Message }
            };
        }
        catch (OperationCanceledException)
        {
            return new ServiceResponseVM<User>
            {
                IsSuccess = false,
                Title = "Failed to register",
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
                Title = "Invalid username or password",
                IsSuccess = false
            };
        }
        var checkPassword = await _userManager.CheckPasswordAsync(existedUser, resource.Password);
        if (!checkPassword)
        {
            return new LoginUserManagement
            {
                Title = "Invalid username or password",
                IsSuccess = false
            };
        }

        if (existedUser.LockoutEnabled)
        {
            return new LoginUserManagement
            {
                Title = "Account is blocked",
                IsSuccess = false
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

    public Task<LoginUserManagement> LoginWithGoogle(string idToken)
    {
        throw new NotImplementedException();
    }
}
