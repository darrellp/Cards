using Simple_Console;
using Spectre.Console;

Random uberRandom = new Random();
while (true)
{
    var seed = uberRandom.Next();
    var board = new KlondikeBoard(seed);
    while (!board.NewGame && !board.Quit) ;
    AnsiConsole.Clear();
    if (board.Quit)
    {
        break;
    }
}