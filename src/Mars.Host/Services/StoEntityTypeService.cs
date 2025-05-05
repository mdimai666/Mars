using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppShared.Models;
using Microsoft.Extensions.Configuration;

namespace Mars.Host.Services;

public class StoEntityTypeService : StandartModelService<StoEntityType>
{
    public StoEntityTypeService(IConfiguration configuration, IServiceProvider serviceProvider) : base(configuration, serviceProvider)
    {
    }
}
