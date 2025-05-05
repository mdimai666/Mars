using System;
using AppShared.Models;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Services;

public class ContactPersonService : StandartModelService<ContactPerson>
{
    public ContactPersonService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }
}

