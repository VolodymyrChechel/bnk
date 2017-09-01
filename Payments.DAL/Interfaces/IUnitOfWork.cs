﻿using System;
using System.Threading.Tasks;
using Payments.DAL.Entities;
using Payments.DAL.Identity;

namespace Payments.DAL.Interfaces
{
    // interface for UnitOfWork pattern
    public interface IUnitOfWork : IDisposable
    {
        // repositories for data store
        IRepository<ClientProfile> ClientProfiles { get; }
        IRepository<Card> Cards { get; }
        IRepository<CreditAccount> CreditAccounts { get; }
        IRepository<DebitAccount> DebitAccounts { get; }
        IRepository<Transaction> Transactions { get; }
        IRepository<UnblockAccountRequest> UnblockAccountRequests { get; }

        // repositories for identity
        ApplicationUserManager UserManager { get; }
        IClientManager ClientManager { get; }
        ApplicationRoleManager RoleManager { get; }
        Task SaveAsync();
    }
}