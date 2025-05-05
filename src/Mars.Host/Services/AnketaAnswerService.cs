using AppShared.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Mars.Host.Services;

public class AnketaAnswerService : StandartModelService<AnketaAnswer>
{
    public AnketaAnswerService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {

    }

    public override Task<AnketaAnswer> Get(Guid id)
    {
        return base.Get(id, s => s.User);
    }
}

