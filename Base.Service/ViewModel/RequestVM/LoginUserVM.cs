using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Service.ViewModel.RequestVM;

public class LoginUserVM
{
    [Required]
    public string UserName { get; set; } = "";

    [Required]
    [MinLength(5)]
    public string Password { get; set; } = "";

    public string? DeviceToken { get; set; }
}
