using System;
using System.Collections.Generic;
using Mars.Host.Shared.Templators;

namespace Mars.Host.Shared.Interfaces;

public interface IDtoMarker
{
    public void Fill(Dictionary<Guid, MetaRelationObjectDict> dataDict);
}
