﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mars.Host.Services;

public static class ExampleDistricts
{
    public static Dictionary<string, string> GetDistricts()
    {

        var list = new Dictionary<string, string>
        {
            ["1"] = "МР «Абыйский улус (район)»",
            ["2"] = "МО «Алданский район»",
            ["3"] = "МО «Аллаиховский улус (район)»",
            ["4"] = "МР «Амгинский улус (район)»",
            ["5"] = "МО «Анабарский национальный (долгано-эвенкийский) улус (район)»",
            ["6"] = "МО «Булунский улус (район)»",
            ["7"] = "МР «Верхневилюйский улус (район)»",
            ["8"] = "МР «Верхнеколымский улус (район)»",
            ["9"] = "МО «Верхоянский район»",
            ["10"] = "МР«Вилюйский улус (район)»",
            ["11"] = "МР «Горный улус»",
            ["12"] = "МР «Жиганский национальный эвенкийский район»",
            ["13"] = "МО «Кобяйский улус (район)»",
            ["14"] = "МО «Ленский район»	",
            ["15"] = "МР «Мегино-Кангаласский улус»",
            ["16"] = "МО «Мирнинский район»",
            ["17"] = "МО «Момский район»",
            ["18"] = "МО «Намский улус»",
            ["19"] = "МО «Нерюнгринский район»",
            ["20"] = "МР «Нижнеколымский район»",
            ["21"] = "МР «Нюрбинский район»",
            ["22"] = "МО «Оймяконский улус (район)»",
            ["23"] = "МР «Олёкминский район»",
            ["24"] = "МР «Оленёкский эвенкийский национальный район»",
            ["25"] = "МО «Среднеколымский улус (район)»",
            ["26"] = "МР «Сунтарский улус (район)»",
            ["27"] = "МР «Таттинский улус»",
            ["28"] = "МР «Томпонский район»",
            ["29"] = "МР «Усть-Алданский улус (район)»",
            ["30"] = "МР «Усть-Майский улус (район)»",
            ["31"] = "МО «Усть-Янский улус (район)»",
            ["32"] = "МР «Хангаласский улус»",
            ["33"] = "МО «Чурапчинский улус (район)»",
            ["34"] = "МО «Эвено-Бытантайский национальный улус (район)»",
            ["35"] = "ГО «Город Якутск»",
            ["36"] = "ГО «Жатай»",
        };

        return list;
    }
}
