using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class MazeRunning : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMSelectable[] Buttons;
   public KMSelectable WaitButton;

   int WhereYouAre = 0;
   int Moves = 0;
   int Goal = 12;
   readonly int[] StartingPosition = { 0, 1, 2, 3, 4, 9, 14, 19, 24, 23, 22, 21, 20, 15, 10, 5, 6, 7, 8, 13, 18, 17, 16, 11, 12 };

   string HorizontalShift = String.Empty;
   string VerticalShift = String.Empty;

   static int moduleIdCounter = 1;
   int moduleId;
   private bool moduleSolved;

   readonly static string[] InitialMaze = {
      "...D", "..UR", "...L", ".URD", "...L",
      ".LUR", ".LDR", ".LDR", ".LUR", ".LDR",
      "..DR", ".LUR", "ULRD", "..LD", "...U",
      ".LUR", ".LDR", ".LUD", "..RU", "..LR",
      "....", "..UD", "..UR", ".LDR", "...L"
   };
   string[] TheMaze = {
      "...D", "..UR", "...L", ".URD", "...L",
      ".LUR", ".LDR", ".LDR", ".LUR", ".LDR",
      "..DR", ".LUR", "ULRD", "..LD", "...U",
      ".LUR", ".LDR", ".LUD", "..RU", "..LR",
      "....", "..UD", "..UR", ".LDR", "...L"
   };
   string[] TempMaze = new string[25];

   void Awake () {
      moduleId = moduleIdCounter++;

      foreach (KMSelectable Button in Buttons) {
         Button.OnInteract += delegate () { ButtonPress(Button); return false; };
      }
      WaitButton.OnInteract += delegate () { WaitingPress(); return false; };
   }

   void Start () {
      //WhereYouAre = (Bomb.GetSerialNumberNumbers().Last() == 0 ? 20 : (Bomb.GetSerialNumberNumbers().Last() - 1) * 10) + (Bomb.GetSerialNumberNumbers().First() == 0 ? 9 : Bomb.GetSerialNumberNumbers().First() - 1);
      WhereYouAre = StartingPosition[Bomb.GetSerialNumberNumbers().ToArray().Sum() % 25];
      //TheMaze[WhereYouAre] += "*";
      LogMaze();
      for (int i = 0; i < 25; i++) {
         TempMaze[i] = "";
      }
      HorizontalShift = Bomb.IsPortPresent(Port.RJ45) ? "LEFT" : "RIGHT";
      VerticalShift = Bomb.GetSerialNumber().Any(x => "AEIOU".Contains(x)) ? "DOWN" : "UP";
   }

   void ButtonPress (KMSelectable Button) {
      Button.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Button.transform);
      for (int i = 0; i < 4; i++) {
         if (Button == Buttons[i]) {
            switch (i) {
               case 0:
                  if (TheMaze[WhereYouAre].Any(x => "D".Contains(x))) {
                     GetComponent<KMBombModule>().HandleStrike();
                  }
                  else {
                     WhereYouAre += 5;
                     WhereYouAre %= 25;
                     Moves++;
                     MazeShifter();
                  }
                  break;
               case 1:
                  if (TheMaze[WhereYouAre].Any(x => "L".Contains(x))) {
                     GetComponent<KMBombModule>().HandleStrike();
                  }
                  else {
                     WhereYouAre--;
                     if (WhereYouAre < 0) {
                        WhereYouAre += 25;
                     }
                     Moves++;
                     MazeShifter();
                  }
                  break;
               case 2:
                  if (TheMaze[WhereYouAre].Any(x => "U".Contains(x))) {
                     GetComponent<KMBombModule>().HandleStrike();
                  }
                  else {
                     WhereYouAre -= 5;
                     if (WhereYouAre < 0) {
                        WhereYouAre += 25;
                     }
                     Moves++;
                     MazeShifter();
                  }
                  break;
               case 3:
                  if (TheMaze[WhereYouAre].Any(x => "R".Contains(x))) {
                     GetComponent<KMBombModule>().HandleStrike();
                  }
                  else {
                     WhereYouAre++;
                     WhereYouAre %= 25;
                     Moves++;
                     MazeShifter();
                  }
                  break;
            }
         }
      }
   }

   void WaitingPress () {
      WaitButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, WaitButton.transform);
      if (WhereYouAre == Goal) {
         GetComponent<KMBombModule>().HandlePass();
         moduleSolved = true;
         return;
      }
      else if (TheMaze[WhereYouAre].Any(x => "U".Contains(x)) && TheMaze[WhereYouAre].Any(x => "D".Contains(x)) && TheMaze[WhereYouAre].Any(x => "L".Contains(x)) && TheMaze[WhereYouAre].Any(x => "R".Contains(x))) {
         Moves++;
         Debug.LogFormat("[Maze Running #{0}] You got stuck, passing a turn...", moduleId);
         MazeShifter();
      }
      else {
         Moves = 0;
         for (int i = 0; i < 25; i++) {
            TheMaze[i] = InitialMaze[i];
         }
         Debug.LogFormat("[Maze Running #{0}] The maze has been reset.", moduleId);
         WhereYouAre = StartingPosition[Bomb.GetSerialNumberNumbers().ToArray().Sum() % 25];
      }
   }

   void MazeShifter () {
      switch (Moves % 2) {
         case 0:
            for (int i = 0; i < 25; i++) {
               if ((i / 5) % 2 == 1) { //Odd rows moving. Has to be even in code since 0-index.
                  TempMaze[i] = HorizontalShift == "LEFT" ? TheMaze[i % 5 == 4 ? i - 4 : i + 1] : TheMaze[i % 5 == 0 ? i + 4 : i - 1];
               }
            }
            for (int i = 0; i < 25; i++) {
               if ((i / 5) % 2 == 0) {
                  if (TheMaze[i].Any(x => "L".Contains(x))) {
                     TempMaze[i] += "L";
                  }
                  if (TheMaze[i].Any(x => "R".Contains(x))) {
                     TempMaze[i] += "R";
                  }
                  if (i < 5) {
                     if (TheMaze[(i - 5 < 0 ? i + 20 : i - 5)].Any(x => "D".Contains(x))) {
                        TempMaze[i] += "U";
                     }
                  }
                  else if (i < 19) {
                     if (TheMaze[(i + 5) % 25].Any(x => "U".Contains(x))) {
                        TempMaze[i] += "D";
                     }
                  }
                  else if (TempMaze[(i - 5 < 0 ? i + 20 : i - 5)].Any(x => "D".Contains(x))) {
                     TempMaze[i] += "U";
                  }
                  else if (TempMaze[(i + 5) % 25].Any(x => "U".Contains(x))) {
                     TempMaze[i] += "D";
                  }
               }
            }
            if ((WhereYouAre / 5) % 2 == 1) {
               if (HorizontalShift == "LEFT") {
                  if (WhereYouAre % 5 == 0) {
                     WhereYouAre += 4;
                  }
                  else {
                     WhereYouAre--;
                  }
               }
               else {
                  if (WhereYouAre % 5 == 4) {
                     WhereYouAre -= 4;
                  }
                  else {
                     WhereYouAre++;
                  }
               }
            }
            if ((Goal / 5) % 2 == 1) {
               if (HorizontalShift == "LEFT") {
                  if (Goal % 5 == 0) {
                     Goal += 4;
                  }
                  else {
                     Goal--;
                  }
               }
               else {
                  if (Goal % 5 == 4) {
                     Goal -= 4;
                  }
                  else {
                     Goal++;
                  }
               }
            }
            for (int i = 0; i < 25; i++) {
               TheMaze[i] = TempMaze[i];
            }
            break;
         case 1:
            for (int i = 0; i < 25; i++) { //Even columns moving.
               if ((i % 5) % 2 == 0) {
                  TempMaze[i] = VerticalShift == "UP" ? TheMaze[(i + 5) % 25] : TheMaze[(i - 5) < 0 ? i + 20 : i - 5];
               }
            }
            for (int i = 0; i < 25; i++) {
               if ((i % 5) % 2 == 1) {
                  if (TheMaze[i].Any(x => "U".Contains(x))) {
                     TempMaze[i] += "U";
                  }
                  if (TheMaze[i].Any(x => "D".Contains(x))) {
                     TempMaze[i] += "D";
                  }
                  if (TempMaze[i + 1].Any(x => "L".Contains(x))) {
                     TempMaze[i] += "R";
                  }
                  if (TempMaze[i - 1].Any(x => "R".Contains(x))) {
                     TempMaze[i] += "L";
                  }
               }
            }
            if (WhereYouAre % 5 % 2 == 0) {
               if (VerticalShift == "DOWN") {
                  WhereYouAre += 5;
                  WhereYouAre %= 25;
               }
               else {
                  WhereYouAre -= 5;
                  if (WhereYouAre < 0) {
                     WhereYouAre += 25;
                  }
               }
            }
            if ((Goal % 5) % 2 == 0) {
               if (VerticalShift == "DOWN") {
                  Goal += 5;
                  Goal %= 25;
               }
               else {
                  Goal -= 5;
                  if (Goal < 0) {
                     Goal += 25;
                  }
               }
            }

            for (int i = 0; i < 25; i++) {
               TheMaze[i] = TempMaze[i];
               while (TheMaze[i].Length != 4) {
                  TheMaze[i] = "." + TheMaze[i];
               }
            }
            break;
      }
      LogMaze();
      for (int i = 0; i < 25; i++) {
         TempMaze[i] = "";
      }
   }

   void LogMaze () {
      Debug.LogFormat("[Maze Running #{0}] After move {1}, the maze is:", moduleId, Moves);
      string Log = "[Maze Running #{0}] ";
      for (int i = 0; i < 25; i++) {
         Log = MakeLog(Log, "U", i);
         Log = MakeLog(Log, "L", i);
         Log = MakeLog(Log, "D", i);
         Log = MakeLog(Log, "R", i);
         if (i == WhereYouAre) {
            Log += "*";
         }
         else {
            Log += ".";
         }
         if (i == Goal) {
            Log += "+";
         }
         else {
            Log += ".";
         }
         Log += " ";
         if (i % 5 == 4 && i != 24) {
            Log += "\n[Maze Running #{0}] ";
         }
      }
      Debug.LogFormat(Log, moduleId);
   }

   String MakeLog (String init, String add, int idx) {
      return init + (TheMaze[idx].Contains(add) ? add : ".");
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} U/L/D/R/Up/Left/Down/Right/Submit/Reset to press that button. Chain with commas.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      string[] Parameters = Command.Trim().ToUpper().Split(',');
      string[] AcceptableCommands = { "U", "D", "L", "R", "LEFT", "RIGHT", "UP", "DOWN", "SUBMIT"};
      int Wrong = 0;
      yield return null;
      for (int i = 0; i < Parameters.Length; i++) {
         Parameters[i] = Parameters[i].Trim();
         for (int j = 0; j < 9; j++) {
            if (Parameters[i] != AcceptableCommands[j]) {
               Wrong++;
            }
            if (Wrong == 9) {
               yield return "sendtochaterror I don't understand!";
               Wrong = 0;
               yield break;
            }
            Wrong = 0;
         }
      }
      for (int i = 0; i < Parameters.Length; i++) {
         switch (Parameters[i]) {
            case "U":
            case "UP":
               Buttons[2].OnInteract();
               break;
            case "D":
            case "DOWN":
               Buttons[0].OnInteract();
               break;
            case "L":
            case "LEFT":
               Buttons[1].OnInteract();
               break;
            case "R":
            case "RIGHT":
               Buttons[3].OnInteract();
               break;
            case "SUBMIT":
               WaitButton.OnInteract();
               break;
         }
         yield return new WaitForSecondsRealtime(.1f);
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      while (!moduleSolved) {
         if (Goal == WhereYouAre || !TheMaze[WhereYouAre].Contains('.')) {
            yield return ProcessTwitchCommand("SUBMIT");
         }
         else if (Goal / 5 < WhereYouAre / 5 && !TheMaze[WhereYouAre].Contains('U')) {
            yield return ProcessTwitchCommand("U");
         }
         else if (Goal / 5 > WhereYouAre / 5 && !TheMaze[WhereYouAre].Contains('D')) {
            yield return ProcessTwitchCommand("D");
         }

         else if (Goal % 5 < WhereYouAre % 5 && !TheMaze[WhereYouAre].Contains('L')) {
            yield return ProcessTwitchCommand("L");
         }
         else if (Goal % 5 > WhereYouAre % 5 && !TheMaze[WhereYouAre].Contains('R')) {
            yield return ProcessTwitchCommand("R");
         }

         List<string> Dir = new List<string>() { "U", "R", "D", "L"};
         for (int i = 0; i < TheMaze[WhereYouAre].Length; i++) {
            Dir.Remove(TheMaze[WhereYouAre][i].ToString());
         }
         Dir.Shuffle();
         yield return ProcessTwitchCommand(Dir[0]);
      }
   }
}
