﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Payments.BLL.DTO;
using Payments.BLL.Infrastructure;

namespace Payments.BLL.Interfaces
{
    // identity service
    // used to interract with DAL
    public interface IUserService : IDisposable
    {
        Task<OperationDetails> Create(UserDTO userDto);
        Task<ClaimsIdentity> Authenticate(UserDTO userDto);
        Task SetInitialData(UserDTO adminDto, UserDTO employeeDto, List<UserDTO> usersDto, List<string> roles);
    }
}