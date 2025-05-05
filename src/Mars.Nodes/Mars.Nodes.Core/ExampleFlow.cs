namespace Mars.Nodes.Core;

public class ExampleFlow
{
    private static string data = @"[
    {
        `id`: `527f15a4.6efdac`,
        `type`: `NodeWorkspace.Nodes.InjectNode`,
        `z`: `52d37040.ea6ab`,
        `name`: ``,
        `props`: [
            {
                `p`: `payload`
            },
            {
                `p`: `topic`,
                `vt`: `str`
            }
        ],
        `repeat`: ``,
        `crontab`: ``,
        `once`: false,
        `onceDelay`: 0.1,
        `topic`: ``,
        `payload`: ``,
        `payloadType`: `date`,
        `x`: 112.5,
        `y`: 163,
        `wires`: [
            [
                `7a75a033.c3c1d`
            ]
        ],
        `color`: `#a6bbcf`
    },
    {
        `id`: `7a75a033.c3c1d`,
        `type`: `NodeWorkspace.Nodes.SwitchNode`,
        `z`: `52d37040.ea6ab`,
        `name`: `case`,
        `func`: `\nreturn msg;`,
        `outputs2`: 4,
        `noerr`: 0,
        `initialize`: ``,
        `finalize`: ``,
        `x`: 281.5,
        `y`: 92,
        `wires`: [
            [
                `ddd19a9c.4b6488`,
                `c2707483.67c6b8`
            ],
            [
                `e89476c7.a171f8`
            ],
            [],
            []
        ],
        `outputLabels`: [
            `ok`,
            ``,
            ``,
            ``
        ]
    },
    {
        `id`: `ddd19a9c.4b6488`,
        `type`: `NodeWorkspace.Nodes.FunctionNode`,
        `z`: `52d37040.ea6ab`,
        `name`: `function1`,
        `func`: `\nreturn msg;`,
        `outputs2`: 1,
        `noerr`: 0,
        `initialize`: ``,
        `finalize`: ``,
        `x`: 577.5,
        `y`: 52,
        `wires`: [
            []
        ]
    },
    {
        `id`: `c2707483.67c6b8`,
        `type`: `NodeWorkspace.Nodes.FunctionNode`,
        `z`: `52d37040.ea6ab`,
        `name`: `function2`,
        `func`: `\nreturn msg;`,
        `outputs2`: 1,
        `noerr`: 0,
        `initialize`: ``,
        `finalize`: ``,
        `x`: 593.5,
        `y`: 133,
        `wires`: [
            []
        ]
    },
    {
        `id`: `e89476c7.a171f8`,
        `type`: `NodeWorkspace.Nodes.FunctionNode`,
        `z`: `52d37040.ea6ab`,
        `name`: `function3`,
        `func`: `\nreturn msg;`,
        `outputs2`: 1,
        `noerr`: 0,
        `initialize`: ``,
        `finalize`: ``,
        `x`: 597,
        `y`: 185,
        `wires`: [
            []
        ]
    }
]";

    public static string GetJsonData()
    {
        return data.Replace('`', '\"');
    }
}
