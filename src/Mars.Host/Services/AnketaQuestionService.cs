using AppShared.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace Mars.Host.Services;

public class AnketaQuestionService : StandartModelService<AnketaQuestion>
{
    public AnketaQuestionService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {

    }

}

