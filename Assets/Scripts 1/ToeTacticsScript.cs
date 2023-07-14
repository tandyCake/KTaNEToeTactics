using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class ToeTacticsScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMColorblindMode Colorblind;
    public KMRuleSeedable Ruleseed;
    //Ordered from the bottom right. (reverse reading order)
    public Tile[] tiles;

    private static string[] tileNames = new[] { "bottom-right", "bottom-middle", "bottom-left", "middle-right", "center", "middle-left", "top-right", "top-middle", "top-left" };

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    private bool moduleInteractable = false;

    private Board startingBoard;
    private Board board;
    private ShapeColor[] shapeColors;
    private SolveState[,] solveStateTable = new SolveState[9, 6];
    private List<SolveState> usedSolveStates = new List<SolveState>();
    SolveState[] allSolveStates = new int[] { 7, 11, 13, 14, 19, 21, 22, 25, 26, 28, 35, 37, 38, 41, 42, 44, 49, 50, 52, 56, 67, 69, 70, 73, 74, 76, 81, 82, 84, 88, 97, 98, 100, 104, 112, 131, 133, 134, 137, 138, 140, 145, 146, 148, 152, 161, 162, 164, 168, 176, 193, 194, 196, 200, 208, 224 }
                   .Select(ix => DecompressState(ix)).ToArray();
    private TileValue playerPiece;
    private static SolveState DecompressState(int ix) {
        List<int> indices = new List<int>(3);
        for (int i = 0; i < 9; i++)
            if ((ix & (1 << i)) != 0)
                indices.Add(i);
        return new SolveState(indices.ToArray());
    }
    void Awake () {
        moduleId = moduleIdCounter++;
        GenRuleseed();
        foreach (Tile tile in tiles)
            tile.selectable.OnInteract += () => { TilePress(tile); return false; };
        Module.OnActivate += () => Activate();
    }
    void Start()
    {
        playerPiece = Bomb.GetSerialNumberNumbers().Last() % 2 == 0 ? TileValue.O : TileValue.X;
        Log("You are playing as {0}.", playerPiece);
    }
    void Activate ()
    {
        GeneratePuzzle();
        moduleInteractable = true;
    }
    void TilePress(Tile tile)
    {
        if (!tile.IsInteractable)
            return;
        tile.selectable.AddInteractionPunch(.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, tile.transform);
        if (moduleInteractable)
        {
            Log("You placed an {0} in the {1} position.", playerPiece, tileNames[tile.position]);
            PlaceTile(tile.position, playerPiece);
        }
    }
    void GenRuleseed()
    {
        var rng = Ruleseed.GetRNG();
        rng.ShuffleFisherYates(allSolveStates);
        for (int rowFromBottom = 0; rowFromBottom < 9; rowFromBottom++)
            for (int col = 0; col < 6; col++)
                solveStateTable[8 - rowFromBottom, col] = allSolveStates[6 * rowFromBottom + col];
        Log("Generated table with rule-seed {0}.", rng.Seed);
    }
    void GeneratePuzzle()
    {
        tiles[2].SetTile(TileValue.O, ShapeColor.Red);
        tiles[3].SetTile(TileValue.X, ShapeColor.Blue);
        tiles[4].SetTile(TileValue.O, ShapeColor.Yellow);
        tiles[8].SetTile(TileValue.X, ShapeColor.Red);
        board = new Board(new TileValue[9], new SolveState[0]);
    }
    void PlaceTile(int position, TileValue piece)
    {
        tiles[position].SetTile(piece, ShapeColor.Gray);
        board[position] = piece;
        CheckCurrentBoard();
    }
    void CheckCurrentBoard()
    {
        TileValue victor = board.GetVictor();
        if (victor == TileValue.None)
            return;
        if (victor == playerPiece)
            Solve();
        else Strike();
    }
    void Solve()
    {
        moduleSolved = true;
        Log("You put three {0}s in a solving pattern. You win!", playerPiece);
        Module.HandlePass();
    }
    void Strike()
    {
        Log("Your opponent put three {0}s in a solving pattern. Strike!", playerPiece == TileValue.X ? 'O' : 'X');
        Module.HandleStrike();
        Reset();
    }
    void Reset()
    {
        for (int i = 0; i < 9; i++)
            tiles[i].SetTile(startingBoard[i], shapeColors[i]);
    }
    SolveState IndexTable(Tile tile)
    {
        return solveStateTable[tile.position,
                    3 * ((int)tile.Shape - 1) + ((int)tile.Color - 1)];
    }
    void Log(string str, params object[] args)
    {
        Debug.LogFormat("[Toe Tactics #{0}] {1}", moduleId, string.Format(str, args));
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} foobar> to do something.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        command = command.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        yield return null;
    }
}
