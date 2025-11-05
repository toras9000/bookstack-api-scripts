#r "nuget: Lestaly.General, 0.108.0"
#nullable enable
using Lestaly;

var settings = new
{
    Docker = new
    {
        Compose = ThisSource.RelativeFile("./docker/compose.yml"),
    },

    Database = new
    {
        Port = (ushort)9985,

        Host = "localhost",

        Database = "bookstack_store",

        Username = "bookstack_user",

        Password = "bookstack_pass",
    },

    BookStack = new
    {
        Url = @"http://localhost:9986/",

        Api = new
        {
            Entry = @"http://localhost:9986/api/",

            TokenName = "TestToken",

            TokenId = "00001111222233334444555566667777",

            TokenSecret = "88889999aaaabbbbccccddddeeeeffff",
        },
    }
};
