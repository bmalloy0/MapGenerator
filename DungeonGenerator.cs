using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DungeonGenerator : MonoBehaviour {
    /*          ALL PROGRAMMING DONE BY B. MALLOY         
       WITH INSPIRATION FROM THE 5E DUNGEON MASTER'S GUIDE 
    */

    /* Explanation of Script
        This is a tile-based map generator, inspired by Dungeons & Dragons 5th Edition
        
        For those unaware, a map can be visualized as a grid, each grid location
        represents a 5-foot square. It is on these maps where many encounters
        take place.
        
        In this script, the map will be represented by a 3-dimensional array "dungeon"
        in which the first dimension indicates a floor number, the second dimension
        represents the x-position of a grid square, and the third dimension represents
        the y-position of a grid square.

        There are many different possible values for each tile/squre, for ease these
        will be utilized as an enumerated variable "Map"

        For ease of communication within the script, a second enumerated valirable "Room"
        will be used to explain to different methods exactly which type of room must be
        validated or placed.

        *****************************************
         
        The issue that I am having is this: For some reason, the validation method herein
        does not accurately determine if a room or passage is valid in a given location.
        Often it does, but it is not 100%, and I am therefore often given finalized dungeons
        with rooms overlapping other rooms or passages, or passages overlapping each other
        or some combination thereof. Sometimes, a room will pass validation, but when it is
        sent to SetRoom(), an "out of bounds" exception is thrown, even though that should be
        the first thing the validation method checks for. I have been over this code multiple
        times and am unable to determine why these things are happening.

        Also, on occasion, some rooms are not setting properly -- they will have gaps, or will
        be offset from where they should be getting placed. Sometimes I am able to figure out 
        why this is happening (I have fixed multiple of these issues), but it still happens and
        I cannot determine why.
    */

    public GameObject[] sprites;
    public int numFloors, mapWidth, mapDepth, xoff, yoff;
    Transform parent;
    float sizer;
    int[] loc;
    int numExits;

    enum Map
    {
        Blank, PassageIP, Door, Stair, Enter, Wall, Room, Passage,
        Well, Pillar, PassageTall, PassageBalcony, WoodDoor, WoodDoorL,
        StoneDoor, StoneDoorL, IronDoor, IronDoorL, Portcullis, PortcullisL,
        SecretDoor, SecretDoorL, FalseDoor, StDwnChm, StDwnPass, StDwn2Chm,
        StDwn2Pass, StDwn3Chm, StDwn3Pass, StUpChm, StUpPass, StUpDead,
        StDwnDead, ChmUpPass, ChmUp2Pass, ShaftDwnChm, ShaftUpDwn, Exit,
        SecretDoorStart
    }
    enum Room
    {
        Start1, Start2, Start3, Start4, Start5, Start6, Start7, Start8,
        Start9, Start0, StndPass, Pass20DoorRt, Pass20DoorLt, Pass20Door,
        Pass20PassRt, Pass20PassLt, Pass20Dead, Pass20L, Pass20R, Chamber,
        Chamber2020, Chamber3030, Chamber4040, Chamber2030O1, Chamber2030O2,
        Chamber3040O1, Chamber3040O2, Chamber4050O1, Chamber4050O2,
        Chamber5080O1, Chamber5080O2, ChamberC30, ChamberC50, ChamberO40,
        ChamberO60, ChamberTrap4060O1, ChamberTrap4060O2, ChamberTrap4060O3,
        ChamberTrap4060O4, BeyondT, BeyondPass, Stair
    }
    enum Direction { Left, Right, Up, Down}

    Map[,,] dungeon;
    
	void Start () {
        //Set a scale for the tile display
        sizer = 0.2f;

        //Set global variable to 0
        numExits = 0;

        //Find and set the display transform
        parent = GameObject.FindGameObjectWithTag("Map Display").GetComponent<Transform>();
    }

    public void NewMap()
    {
        //For debugging, clear the log for each map for ease of reading
        ClearLog();

        //Reset the global pointer variable
        //loc[0] = floor number
        //loc[1] = x location
        //loc[2] = y location
        loc = new int[] { 0, 0, 0 };

        //Clear out the old map
        foreach (DestroyMe tile in FindObjectsOfType<DestroyMe>())
        {
            tile.Destroy();
        }

        //If a map's dimensions are too small, set them to default values
        if (numFloors < 1)
            numFloors = 1;
        if (mapWidth < 25)
            mapWidth = 25;
        if (mapDepth < 25)
            mapDepth = 25;

        //Reset the global map
        //dungeon[0] = floor number
        //dungeon[1] = X location
        //dungeon[2] = Y location
        dungeon = new Map[numFloors, mapWidth, mapDepth];

        //Set each value of the new map to Blank
        for (int f = 0; f < numFloors; f++)
            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapDepth; y++)
                    dungeon[f, x, y] = Map.Blank;

        //Figure out which enterance you start with
        RollEnterance();

        //Search for tiles that designate an incomplete map
        //Do not continue until none are found
        while (Incomplete())
        {
            //If the starting point is close to the edge of the map
            //Just set to a standard tile and skip
            //Otherwise, figure out what to do at that tile
            if (loc[1] <= 1 || loc[2] <= 1)
            {
                if (dungeon[loc[0], loc[1], loc[2]] == Map.PassageIP)
                    dungeon[loc[0], loc[1], loc[2]] = Map.Passage;
                else if (dungeon[loc[0], loc[1], loc[2]] == Map.Door)
                    dungeon[loc[0], loc[1], loc[2]] = Map.WoodDoor;
                else
                    dungeon[loc[0], loc[1], loc[2]] = Map.SecretDoor;
            }
            else if ((loc[1] >= (mapWidth - 2)) || (loc[2] >= (mapDepth - 2)))
            {
                if (dungeon[loc[0], loc[1], loc[2]] == Map.PassageIP)
                    dungeon[loc[0], loc[1], loc[2]] = Map.Passage;
                else if (dungeon[loc[0], loc[1], loc[2]] == Map.Door)
                    dungeon[loc[0], loc[1], loc[2]] = Map.WoodDoor;
                else
                    dungeon[loc[0], loc[1], loc[2]] = Map.SecretDoor;
            }
            else
            {
                //If the location of the incomplete dungeon is a PassageIP
                //Roll a passage
                //Otherwise, roll a door
                switch (dungeon[loc[0], loc[1], loc[2]])
                {
                    case Map.PassageIP:
                        RollPassage();
                        break;
                    case Map.Door:
                    case Map.SecretDoor:
                        BeyondDoor();
                        break;
                }
            }
        }

        //Once the dungeon has been mapped out
        //Place walls around all the tiles
        FindWalls();

        //Once walls have been placed, display the map for the user
        DisplayMap();
    }

    static void ClearLog()
    {
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");

        var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);

        clearMethod.Invoke(null, null);
    }

    void RollEnterance()
    {
        //Get a random number between 1 and 10 (inclusive)
        //Depending on the number, set a corresponding room
        switch(Random.Range(1, 11))
        {
            case 1:
                SetRoom(Room.Start1, 0, Direction.Down);
                break;
            case 2:
                SetRoom(Room.Start2, 0, Direction.Down);
                break;
            case 3:
                SetRoom(Room.Start3, 0, Direction.Down);
                break;
            case 4:
                SetRoom(Room.Start4, 0, Direction.Down);
                break;
            case 5:
                SetRoom(Room.Start5, 0, Direction.Down);
                break;
            case 6:
                SetRoom(Room.Start6, 0, Direction.Down);
                break;
            case 7:
                SetRoom(Room.Start7, 0, Direction.Down);
                break;
            case 8:
                SetRoom(Room.Start8, 0, Direction.Down);
                break;
            case 9:
                SetRoom(Room.Start9, 0, Direction.Down);
                break;
            case 10:
                SetRoom(Room.Start0, 0, Direction.Down);
                break;
        }
    }

    void SetRoom(Room input, int size, Direction dir)
    {
        //Set the middle tile (only relevant for enterance)
        int mid = mapWidth / 2;

        switch (input)
        {
            //All rooms, passages, etc are centered on the starting location

            #region Enter
            //For these rooms, the placement will be the same starting area
            //Build the room out from there. 
            
            //Start1 = a 4-tile by 4-tile square room, a passage on each wall
            case Room.Start1:
                for (int x = mid - 1; x < mid + 3; x++)
                    for (int y = 1; y < 5; y++)
                        dungeon[0, x, y] = Map.Room;
                dungeon[0, mid - 2, 3] = Map.PassageIP;
                dungeon[0, mid + 3, 3] = Map.PassageIP;
                dungeon[0, mid + 0, 5] = Map.PassageIP;
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                break;

            //Start2 = a 4-tile by 4-tile square room, a door on two walls,
            //and a passage on the third wall
            case Room.Start2:
                for (int x = mid - 1; x < mid + 3; x++)
                    for (int y = 1; y < 5; y++)
                        dungeon[0, x, y] = Map.Room;
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                switch (Random.Range(1, 4))
                {
                    case 1:
                        dungeon[0, mid - 2, 3] = Map.PassageIP;
                        dungeon[0, mid + 3, 3] = Map.Door;
                        dungeon[0, mid + 0, 5] = Map.Door;
                        break;
                    case 2:
                        dungeon[0, mid - 2, 3] = Map.Door;
                        dungeon[0, mid + 3, 3] = Map.PassageIP;
                        dungeon[0, mid + 0, 5] = Map.Door;
                        break;
                    case 3:
                        dungeon[0, mid - 2, 3] = Map.Door;
                        dungeon[0, mid + 3, 3] = Map.Door;
                        dungeon[0, mid + 0, 5] = Map.PassageIP;
                        break;
                }
                break;

            //Start3 = an 8-tile by 8-tile square room, a door on each wall
            case Room.Start3:
                for (int x = mid - 3; x < mid + 5; x++)
                    for (int y = 1; y < 9; y++)
                        dungeon[0, x, y] = Map.Room;
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                dungeon[0, mid - 4, 5] = Map.Door;
                dungeon[0, mid + 5, 5] = Map.Door;
                dungeon[0, mid + 0, 9] = Map.Door;
                break;

            //Start4 = a 16-tile by 4-tile rectangular room, with a row of pillars
            //A passage on each long wall, and a door on the short wall
            case Room.Start4:
                for (int x = mid - 1; x < mid + 3; x++)
                    for (int y = 1; y < 17; y++)
                    {
                        if ((x == mid) || (x == mid + 1))
                            dungeon[0, x, y] = Map.Pillar;
                        else
                            dungeon[0, x, y] = Map.Room;
                    }
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                dungeon[0, mid - 2, 9] = Map.PassageIP;
                dungeon[0, mid + 3, 9] = Map.PassageIP;
                dungeon[0, mid + 0, 17] = Map.Door;
                break;

            //Start5 = a 4-tile by 8-tile rectangular room, with a passage on each wall
            case Room.Start5:
                for (int x = mid - 3; x < mid + 5; x++)
                    for (int y = 1; y < 5; y++)
                        dungeon[0, x, y] = Map.Room;
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                dungeon[0, mid - 4, 3] = Map.PassageIP;
                dungeon[0, mid + 0, 5] = Map.PassageIP;
                dungeon[0, mid + 5, 3] = Map.PassageIP;
                break;

            //Start6 = an 8-tile diameter circle, one passage in each direction
            case Room.Start6:
                for (int x = mid - 3; x < mid + 5; x++)
                {
                    if ((x == mid - 3) || (x == mid + 4))
                        for (int y = 3; y < 7; y++)
                            dungeon[0, x, y] = Map.Room;
                    else if ((x == mid - 2) || (x == mid + 3))
                        for (int y = 2; y < 8; y++)
                            dungeon[0, x, y] = Map.Room;
                    else
                        for (int y = 1; y < 9; y++)
                            dungeon[0, x, y] = Map.Room;
                }
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                dungeon[0, mid - 4, 5] = Map.PassageIP;
                dungeon[0, mid + 5, 5] = Map.PassageIP;
                dungeon[0, mid + 0, 9] = Map.PassageIP;
                break;

            //Start7 = an 8-tile diameter circle, one passage in each direction
            //and a well in the middle of the room
            case Room.Start7:
                for (int x = mid - 3; x < mid + 5; x++)
                {
                    if ((x == mid - 3) || (x == mid + 4))
                        for (int y = 3; y < 7; y++)
                            dungeon[0, x, y] = Map.Room;
                    else if ((x == mid - 2) || (x == mid + 3))
                        for (int y = 2; y < 8; y++)
                            dungeon[0, x, y] = Map.Room;
                    else
                        for (int y = 1; y < 9; y++)
                            dungeon[0, x, y] = Map.Room;
                }
                for (int x = mid; x < mid + 2; x++)
                    for (int y = 4; y < 6; y++)
                        dungeon[0, x, y] = Map.Well;
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                dungeon[0, mid - 4, 5] = Map.PassageIP;
                dungeon[0, mid + 5, 5] = Map.PassageIP;
                dungeon[0, mid + 0, 9] = Map.PassageIP;
                break;

            //Start8 = a 4-tile by 4-tile square room, a door on two walls (one secret)
            //and a passage in the remaining wall
            case Room.Start8:
                for (int x = mid - 1; x < mid + 3; x++)
                    for (int y = 1; y < 5; y++)
                        dungeon[0, x, y] = Map.Room;
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                switch (Random.Range(1, 4))
                {
                    case 1:
                        dungeon[0, mid - 2, 3] = Map.Door;
                        dungeon[0, mid + 3, 3] = Map.Door;
                        dungeon[0, mid + 0, 5] = Map.SecretDoorStart;
                        break;
                    case 2:
                        dungeon[0, mid - 2, 3] = Map.Door;
                        dungeon[0, mid + 3, 3] = Map.SecretDoorStart;
                        dungeon[0, mid + 0, 5] = Map.Door;
                        break;
                    case 3:
                        dungeon[0, mid - 2, 3] = Map.SecretDoorStart;
                        dungeon[0, mid + 3, 3] = Map.Door;
                        dungeon[0, mid + 0, 5] = Map.Door;
                        break;
                }
                break;

            //Start9 = a 2-tile wide passage that ends in a T intersection
            case Room.Start9:
                for (int x = mid - 2; x < mid + 4; x++)
                {
                    if ((x == mid) || (x == mid + 1))
                        for (int y = 1; y < 5; y++)
                            dungeon[0, x, y] = Map.Passage;
                    else
                        for (int y = 3; y < 5; y++)
                            dungeon[0, x, y] = Map.Passage;
                }
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                dungeon[0, mid - 3, 4] = Map.PassageIP;
                dungeon[0, mid + 4, 4] = Map.PassageIP;
                break;

            //Start0 = a 2-tile wide passage that ends in a 4-way intersection
            case Room.Start0:
                for (int x = mid - 2; x < mid + 4; x++)
                {
                    if ((x == mid) || (x == mid + 1))
                        for (int y = 1; y < 7; y++)
                            dungeon[0, x, y] = Map.Passage;
                    else
                        for (int y = 3; y < 5; y++)
                            dungeon[0, x, y] = Map.Passage;
                }
                dungeon[0, mid + 0, 0] = Map.Enter;
                dungeon[0, mid + 1, 0] = Map.Enter;
                dungeon[0, mid - 3, 4] = Map.PassageIP;
                dungeon[0, mid + 4, 4] = Map.PassageIP;
                dungeon[0, mid + 0, 7] = Map.PassageIP;
                break;
            #endregion
            #region Passages
            //Passages can be a variety of different sizes and shapes
            //When going left or right, even-tile-sized passages are shifted
            //Down one tile to center them on the host passage or room
            //When going up or down, even-tile-sized passages are shifted
            //Right one tile to center them on the host passage or room

            //Passages with "size" 8 or more are all 8 tiles wide, but have
            //Different aspects about their interiors:
            //8 = 8 tiles wide with a row of pillars down the middle
            //9 = 8 tiles wide with two rows of pillars
            //10 = 8 tiles wide, but is a taller than normal passage
            //11 = 8 tiles wide, with a balcony around the edge of the passage

            case Room.StndPass:
            case Room.Pass20DoorLt:
            case Room.Pass20DoorRt:
            case Room.Pass20PassLt:
            case Room.Pass20PassRt:
                //All of these passages have the same basic shape (6 tiles long)
                //The only difference is where and how they branch (set later)
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2]; y < loc[2] + 6; y++)
                            if (size == 1)
                                dungeon[loc[0], loc[1], y] = Map.Passage;
                            else if (size < 8)
                                for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 1; x++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (size == 8)
                                        if ((x == loc[1]) || (x == loc[1] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 9)
                                        if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 10)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                    else
                                    {
                                        if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    }
                        dungeon[loc[0], loc[1], loc[2] + 6] = Map.PassageIP;
                        break;
                    case Direction.Up:
                        for (int y = loc[2]; y > loc[2] - 6; y--)
                            if (size == 1)
                                dungeon[loc[0], loc[1], y] = Map.Passage;
                            else if (size < 8)
                                for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 1; x++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (size == 8)
                                        if ((x == loc[1]) || (x == loc[1] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 9)
                                        if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 10)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                    else
                                    {
                                        if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    }
                        dungeon[loc[0], loc[1], loc[2] - 6] = Map.PassageIP;
                        break;
                    case Direction.Right:
                        for (int x = loc[1]; x < loc[1] + 6; x++)
                            if (size == 1)
                                dungeon[loc[0], x, loc[2]] = Map.Passage;
                            else if (size < 8)
                                for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2); y++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (size == 8)
                                        if ((y == loc[2]) || (y == loc[2] - 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 9)
                                        if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 10)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                    else
                                    {
                                        if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    }
                        dungeon[loc[0], loc[1] + 6, loc[2]] = Map.PassageIP;
                        break;
                    case Direction.Left:
                        for (int x = loc[1]; x > loc[1] - 6; x--)
                            if (size == 1)
                                dungeon[loc[0], x, loc[2]] = Map.Passage;
                            else if (size < 8)
                                for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2); y++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (size == 8)
                                        if ((y == loc[2]) || (y == loc[2] - 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 9)
                                        if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 10)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                    else
                                    {
                                        if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    }
                        dungeon[loc[0], loc[1] - 6, loc[2]] = Map.PassageIP;
                        break;
                }
                break;

            case Room.Pass20Dead:
            case Room.Pass20Door:
                //These passages have the same basic shape (4 tiles long)
                //The only difference is the end (set later)
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2]; y < loc[2] + 4; y++)
                            if (size == 1)
                                dungeon[loc[0], loc[1], y] = Map.Passage;
                            else if (size < 8)
                                for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 1; x++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (size == 8)
                                        if ((x == loc[1]) || (x == loc[1] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 9)
                                        if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 10)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                    else
                                    {
                                        if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    }
                        break;
                    case Direction.Up:
                        for (int y = loc[2]; y > loc[2] - 4; y--)
                            if (size == 1)
                                dungeon[loc[0], loc[1], y] = Map.Passage;
                            else if (size < 8)
                                for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 1; x++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (size == 8)
                                        if ((x == loc[1]) || (x == loc[1] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 9)
                                        if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 10)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                    else
                                    {
                                        if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    }
                        break;
                    case Direction.Right:
                        for (int x = loc[1]; x < loc[1] + 4; x++)
                            if (size == 1)
                                dungeon[loc[0], x, loc[2]] = Map.Passage;
                            else if (size < 8)
                                for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2); y++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (size == 8)
                                        if ((y == loc[2]) || (y == loc[2] - 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 9)
                                        if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 10)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                    else
                                    {
                                        if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    }
                        break;
                    case Direction.Left:
                        for (int x = loc[1]; x > loc[1] - 4; x--)
                            if (size == 1)
                                dungeon[loc[0], x, loc[2]] = Map.Passage;
                            else if (size < 8)
                                for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2); y++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (size == 8)
                                        if ((y == loc[2]) || (y == loc[2] - 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 9)
                                        if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    else if (size == 10)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                    else
                                    {
                                        if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                    }
                        break;
                }
                break;

            case Room.Pass20L:
                //This passage continues straight for 4 tiles, then turns left
                //And continues for an additional 2 tiles. The passage width after
                //The turn is the same as before (pillars and balconies follow the turn)
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                        {
                            for (int y = loc[2]; y < loc[2] + 5; y++)
                                if (y < loc[2] + 4)
                                    dungeon[loc[0], loc[1], y] = Map.Passage;
                                else
                                    for (int x = loc[1]; x < loc[1] + 3; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 3, loc[2] + 4] = Map.PassageIP;
                        }
                        else if (size < 8)
                        {
                            for (int y = loc[2]; y < loc[2] + 4 + size; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 1; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 3; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + (size / 2) + 3, loc[2] + 4 + (size / 2)] = Map.PassageIP;
                        }
                        else if (size == 8)
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x == loc[1]) || (x == loc[1] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        if (y < loc[2] + 7)
                                            if ((x == loc[1]) || (x == loc[1] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y < loc[2] + 9)
                                            if (x < loc[1] - 1)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;

                            dungeon[loc[0], loc[1] + 7, loc[2] + 8] = Map.PassageIP;
                        }
                        else if (size == 9)
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        if (y < loc[2] + 7)
                                            if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y < loc[2] + 9)
                                            if (x == loc[1] - 1)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y < loc[2] + 10)
                                            if (x > loc[1] - 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Pillar;
                            dungeon[loc[0], loc[1] + 7, loc[2] + 8] = Map.PassageIP;
                        }
                        else if (size == 11)
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        if (y < loc[2] + 6)
                                        {
                                            if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        }
                                        else if (y < loc[2] + 10)
                                        {
                                            if (x < loc[1] - 1)
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        }
                                        else
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                            dungeon[loc[0], loc[1] + 7, loc[2] + 8] = Map.PassageIP;
                        }
                        else
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                            dungeon[loc[0], loc[1] + 7, loc[2] + 8] = Map.PassageIP;
                        }
                        break;
                    case Direction.Up:
                        if (size == 1)
                        {
                            for (int y = loc[2]; y > loc[2] - 5; y--)
                                if (y > loc[2] - 4)
                                    dungeon[loc[0], loc[1], y] = Map.Passage;
                                else
                                    for (int x = loc[1]; x > loc[1] - 3; x--)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 3, loc[2] - 4] = Map.PassageIP;
                        }
                        else if (size < 8)
                        {
                            for (int y = loc[2]; y > loc[2] - 4 - size; y--)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 1; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - ((size / 2) - 1) - 2; x < loc[1] + (size / 2) + 1; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - (size / 2) - 3, loc[2] - 3 - (size / 2)] = Map.PassageIP;
                        }
                        else if (size == 8)
                        {
                            for (int y = loc[2]; y > loc[2] - 12; y--)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x == loc[1]) || (x == loc[1] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 5; x < loc[1] + 5; x++)
                                        if (y > loc[2] - 7)
                                            if ((x == loc[1]) || (x == loc[1] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y > loc[2] - 9)
                                            if (x < loc[1] + 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 6, loc[2] - 7] = Map.PassageIP;
                        }
                        else if (size == 9)
                        {
                            for (int y = loc[2]; y > loc[2] - 12; y--)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 5; x < loc[1] + 5; x++)
                                        if (y > loc[2] - 6)
                                            if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y == loc[2] - 6)
                                            if ((x < loc[1]) || (x == loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y > loc[2] - 9)
                                            if (x == loc[2] + 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y == loc[2] - 9)
                                            if (x < loc[2] + 3)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 6, loc[2] - 7] = Map.PassageIP;
                        }
                        else if (size == 11)
                        {
                            for (int y = loc[2]; y > loc[2] - 12; y--)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        if (y > loc[2] - 6)
                                        {
                                            if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        }
                                        else if (y > loc[2] - 10)
                                        {
                                            if (x > loc[1] + 2)
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        }
                                        else
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                            dungeon[loc[0], loc[1] - 6, loc[2] - 7] = Map.PassageIP;
                        }
                        else
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                            dungeon[loc[0], loc[1] + 7, loc[2] - 8] = Map.PassageIP;
                        }
                        break;
                    case Direction.Right:
                        if (size == 1)
                        {
                            for (int x = loc[1]; x < loc[1] + 5; x++)
                                if (x < loc[1] + 4)
                                    dungeon[loc[0], x, loc[2]] = Map.Passage;
                                else
                                    for (int y = loc[2]; y > loc[2] - 3; y--)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 4, loc[2] - 3] = Map.PassageIP;
                        }
                        else if (size < 8)
                        {
                            for (int x = loc[1]; x < loc[1] + size + 4; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2); y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - (size / 2) - 2; y < loc[2] + (size / 2); y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 3 + (size / 2), loc[2] - (size / 2) - 3] = Map.PassageIP;
                        }
                        else if (size == 8)
                        {
                            for (int x = loc[1]; x < loc[1] + 12; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y == loc[2] - 1) || (y == loc[2]))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 6; y < loc[2] + 4; y++)
                                        if (x < loc[1] + 7)
                                            if ((y == loc[2] - 1) || (y == loc[2]))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x < loc[1] + 9)
                                            if (y < loc[2] + 1)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 7, loc[2] - 7] = Map.PassageIP;
                        }
                        else if (size == 9)
                        {
                            for (int x = loc[1]; x < loc[1] + 12; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 6; y < loc[2] + 4; y++)
                                        if (x < loc[1] + 6)
                                            if ((y == loc[2] + 1) || (y == loc[2] - 2))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x == loc[1] + 6)
                                            if ((y == loc[2] + 1) || (y < loc[2] - 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x < loc[1] + 9)
                                            if (y == loc[2] + 1)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x == loc[1] + 9)
                                            if (y < loc[2] + 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 7, loc[2] - 7] = Map.PassageIP;
                        }
                        else if (size == 10)
                        {
                            for (int x = loc[1]; x < loc[1] + 12; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                else
                                    for (int y = loc[2] - 6; y < loc[2] + 4; y++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                        }
                        else
                        {
                            for (int x = loc[1]; x < loc[1] + 12; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 6; y < loc[2] + 4; y++)
                                        if (x < loc[1] + 6)
                                        {
                                            if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        }
                                        else if (x < loc[1] + 10)
                                        {
                                            if (y > loc[2] + 1)
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        }
                                        else
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                        }
                        break;
                    case Direction.Left:
                        if (size == 1)
                        {
                            for (int x = loc[1]; x > loc[1] - 5; x--)
                                if (x > loc[1] - 4)
                                    dungeon[loc[0], x, loc[2]] = Map.Passage;
                                else
                                    for (int y = loc[2]; y < loc[2] + 3; y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 4, loc[2] + 3] = Map.PassageIP;
                        }
                        else if (size < 8)
                        {
                            for (int x = loc[1]; x > loc[1] - 4 - size; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2); y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2) + 2; y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 4 - (size / 2), loc[2] + (size / 2) + 2] = Map.PassageIP;
                        }
                        else if (size == 8)
                        {
                            for (int x = loc[1]; x > loc[1] - 12; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y == loc[2] - 1) || (y == loc[2]))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 4; y < loc[2] + 6; y++)
                                        if (x > loc[1] - 7)
                                            if ((y == loc[2] - 1) || (y == loc[2]))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x > loc[1] - 9)
                                            if (y > loc[2] - 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 8, loc[2] - 6] = Map.PassageIP;
                        }
                        else if (size == 9)
                        {
                            for (int x = loc[1]; x > loc[1] - 12; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 4; y < loc[2] + 6; y++)
                                        if (x > loc[1] - 6)
                                            if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x == loc[1] - 6)
                                            if ((y == loc[2] - 2) || (y >= loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x > loc[1] - 9)
                                            if (y == loc[2] - 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x == loc[1] - 9)
                                            if (y >= loc[2] - 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 8, loc[2] - 6] = Map.PassageIP;
                        }
                        else if (size == 10)
                        {
                            for (int x = loc[1]; x > loc[1] - 12; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                else
                                    for (int y = loc[2] - 4; y < loc[2] + 6; y++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                            dungeon[loc[0], loc[1] - 8, loc[2] - 6] = Map.PassageIP;
                        }
                        else
                        {
                            for (int x = loc[1]; x > loc[1] - 12; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 4; y < loc[2] + 6; y++)
                                        if (x > loc[1] - 6)
                                            if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x > loc[1] - 10)
                                            if (y < loc[2] - 2)
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                        }
                        break;
                }
                break;

            case Room.Pass20R:
                //This passage continues straight for 4 tiles, then turns right
                //And continues for an additional 2 tiles. The passage width after
                //The turn is the same as before (pillars and balconies follow the turn)
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                        {
                            for (int y = loc[2]; y < loc[2] + 5; y++)
                                if (y < loc[2] + 4)
                                    dungeon[loc[0], loc[1], y] = Map.Passage;
                                else
                                    for (int x = loc[1]; x > loc[1] - 3; x--)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 3, loc[2] + 4] = Map.PassageIP;
                        }
                        else if (size < 8)
                        {
                            for (int y = loc[2]; y < loc[2] + 4 + size; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 1; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - ((size / 2) - 1) - 2; x < loc[1] + (size / 2) + 1; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 2 - (size / 2), loc[2] + 4 + (size / 2)] = Map.PassageIP;
                        }
                        else if (size == 8)
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x == loc[1]) || (x == loc[1] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 5; x < loc[1] + 5; x++)
                                        if (y < loc[2] + 7)
                                            if ((x == loc[1]) || (x == loc[1] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y < loc[2] + 9)
                                            if (x < loc[1] + 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 6, loc[2] + 8] = Map.PassageIP;
                        }
                        else if (size == 9)
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 5; x < loc[1] + 5; x++)
                                        if (y < loc[2] + 6)
                                            if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y == loc[2] + 6)
                                            if ((x <= loc[1] - 1) || (x == loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y < loc[2] + 9)
                                            if (x == loc[1] + 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y == loc[2] + 9)
                                            if (x <= loc[1] + 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 6, loc[2] + 8] = Map.PassageIP;
                        }
                        else if (size == 10)
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                else
                                    for (int x = loc[1] - 5; x < loc[1] + 5; x++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                            dungeon[loc[0], loc[1] - 6, loc[2] + 8] = Map.PassageIP;
                        }
                        else
                        {
                            for (int y = loc[2]; y < loc[2] + 12; y++)
                                if (y < loc[2] + 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 5; x < loc[1] + 5; x++)
                                        if (y < loc[2] + 6)
                                            if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y < loc[2] + 10)
                                            if (x > loc[1] + 2)
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                        }
                        break;
                    case Direction.Up:
                        if (size == 1)
                        {
                            for (int y = loc[2]; y > loc[2] - 5; y--)
                                if (y > loc[2] - 4)
                                    dungeon[loc[0], loc[1], y] = Map.Passage;
                                else
                                    for (int x = loc[1]; x < loc[1] + 3; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 3, loc[2] - 4] = Map.PassageIP;
                        }
                        else if (size < 8)
                        {
                            for (int y = loc[2]; y > loc[2] - 4 - size; y--)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 1; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - ((size / 2) - 1); x < loc[1] + (size / 2) + 3; x++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + (size / 2) + 2, loc[2] + (size / 2) + 3] = Map.PassageIP;
                        }
                        else if (size == 8)
                        {
                            for (int y = loc[2]; y > loc[2] - 12; y--)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x == loc[1]) || (x == loc[1] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        if (y > loc[2] - 7)
                                            if ((x == loc[1]) || (x == loc[1] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y > loc[2] - 9)
                                            if (x >= loc[1])
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 7, loc[1] - 7] = Map.PassageIP;
                        }
                        else if (size == 9)
                        {
                            for (int y = loc[2]; y > loc[2] - 12; y--)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        if (y > loc[2] - 6)
                                            if ((x == loc[1] - 1) || (x == loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y == loc[2] - 6)
                                            if ((x == loc[1] - 1) || (x >= loc[1] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y > loc[2] - 9)
                                            if (x == loc[1] - 1)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y == loc[2] - 9)
                                            if (x >= loc[1] - 1)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 7, loc[1] - 7] = Map.PassageIP;
                        }
                        else if (size == 11)
                        {
                            for (int y = loc[2]; y > loc[2] - 12; y++)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        if (y > loc[2] - 6)
                                            if ((x < loc[1] - 1) || (x > loc[1] + 2))
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (y > loc[2] - 10)
                                            if (x < loc[1] - 1)
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                            dungeon[loc[0], loc[1] + 7, loc[1] - 7] = Map.PassageIP;
                        }
                        else
                        {
                            for (int y = loc[2]; y > loc[2] - 12; y--)
                                if (y > loc[2] - 4)
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 7; x++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                            dungeon[loc[0], loc[1] + 7, loc[1] - 7] = Map.PassageIP;
                        }
                        break;
                    case Direction.Right:
                        if (size == 1)
                        {
                            for (int x = loc[1]; x < loc[1] + 5; x++)
                                if (x < loc[1] + 4)
                                    dungeon[loc[0], x, loc[2]] = Map.Passage;
                                else
                                    for (int y = loc[2]; y < loc[2] + 3; y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 4, loc[2] + 3] = Map.PassageIP;
                        }
                        else if (size < 8)
                        {
                            for (int x = loc[1]; x < loc[1] + 4 + size; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2); y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2) + 2; y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 4 - (size / 2), loc[2] - (size / 2) - 2] = Map.PassageIP;
                        }
                        else if (size == 8)
                        {
                            for (int x = loc[1]; x < loc[1] + 12; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y == loc[2] - 1) || (y == loc[2]))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 4; y < loc[2] + 6; y++)
                                        if (x < loc[1] + 7)
                                            if ((y == loc[2] - 1) || (y == loc[2]))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x < loc[1] + 9)
                                            if (y >= loc[2] - 1)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 8, loc[2] + 6] = Map.PassageIP;
                        }
                        else if (size == 9)
                        {
                            for (int x = loc[1]; x < loc[1] + 12; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 4; y < loc[2] + 6; y++)
                                        if (x < loc[1] + 7)
                                            if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x == loc[1] + 7)
                                            if ((y == loc[2] - 2) || (y >= loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x < loc[1] + 9)
                                            if (y == loc[2] - 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x == loc[1] + 9)
                                            if (y >= loc[2] - 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 8, loc[2] + 6] = Map.PassageIP;
                        }
                        else if (size == 10)
                        {
                            for (int x = loc[1]; x < loc[1] + 12; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                else
                                    for (int y = loc[2] - 4; y < loc[2] + 6; y++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                        }
                        else
                        {
                            for (int x = loc[1]; x < loc[1] + 12; x++)
                                if (x < loc[1] + 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 4; y < loc[2] + 6; y++)
                                        if (x < loc[1] + 6)
                                            if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x < loc[1] + 10)
                                            if (y < loc[2] - 2)
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                            dungeon[loc[0], loc[1] - 8, loc[2] + 6] = Map.PassageIP;
                        }
                        break;
                    case Direction.Left:
                        if (size == 1)
                        {
                            for (int x = loc[1]; x > loc[1] - 5; x--)
                                if (x > loc[1] - 4)
                                    dungeon[loc[0], x, loc[2]] = Map.Passage;
                                else
                                    for (int y = loc[2]; y > loc[2] - 3; y--)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 4, loc[2] - 3] = Map.PassageIP;
                        }
                        else if (size < 8)
                        {
                            for (int x = loc[1]; x > loc[1] - 4 - size; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - (size / 2); y < loc[2] + (size / 2); y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - (size / 2) - 2; y < loc[2] + (size / 2); y++)
                                        dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 4 - (size / 2), loc[2] - 3 - (size / 2)] = Map.PassageIP;
                        }
                        else if (size == 8)
                        {
                            for (int x = loc[1]; x > loc[1] - 12; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y == loc[2] - 2) || (y == loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 6; y < loc[2] + 4; y++)
                                        if (x == loc[1] - 6)
                                            if ((y < loc[2] - 1) || (y == loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x > loc[1] - 9)
                                            if (y == loc[2] + 1)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x == loc[1] - 9)
                                            if (y < loc[2] + 2)
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 8, loc[2] - 7] = Map.PassageIP;
                        }
                        else if (size == 9)
                        {
                            for (int x = loc[1]; x > loc[1] - 12; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y == loc[2] - 1) || (y == loc[2]))
                                            dungeon[loc[0], x, y] = Map.Pillar;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 6; y < loc[2] + 4; y++)
                                        if ((x == loc[1] - 6) || (x == loc[1] - 7))
                                            if (y <= loc[2])
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x < loc[1] - 6)
                                        {
                                            if ((y == loc[2] - 1) || (y == loc[2]))
                                                dungeon[loc[0], x, y] = Map.Pillar;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        }
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                            dungeon[loc[0], loc[1] - 8, loc[2] - 7] = Map.PassageIP;
                        }
                        else if (size == 10)
                        {
                            for (int x = loc[1]; x > loc[1] - 12; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                                else
                                    for (int y = loc[2] - 6; y < loc[2] + 4; y++)
                                        dungeon[loc[0], x, y] = Map.PassageTall;
                        }
                        else
                        {
                            for (int x = loc[1]; x > loc[1] - 12; x--)
                                if (x > loc[1] - 4)
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                                        else
                                            dungeon[loc[0], x, y] = Map.Passage;
                                else
                                    for (int y = loc[2] - 6; y < loc[2] + 4; y++)
                                        if (x > loc[1] - 6)
                                            if ((y < loc[2] - 2) || (y > loc[2] + 1))
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else if (x > loc[1] - 10)
                                            if (y > loc[2] + 1)
                                                dungeon[loc[0], x, y] = Map.PassageBalcony;
                                            else
                                                dungeon[loc[0], x, y] = Map.Passage;
                                        else
                                            dungeon[loc[0], x, y] = Map.PassageBalcony;
                        }
                        break;
                }
                break;
            #endregion
            #region Chambers
            //Chambers are offset by one tile in the direction they go --
            //This allows doors to exist without interrupting the room or
            //Parent room or passage. Depending on the size of the room,
            //They may have a different number of exits (doors or corridors)

            //Exits not currently coded

            case Room.Chamber2020:
                //A square room, 4 tiles by 4 tiles. 
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 5; y++)
                            for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 5; y--)
                            for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 5; x--)
                            for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 5; x++)
                            for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber2030O1:
                //A rectangular room, 4 tiles by 6 tiles. 
                //Enter on the small wall. 
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 7; y++)
                            for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 7; y--)
                            for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 7; x--)
                            for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 7; x++)
                            for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber2030O2:
                //A rectangular room, 4 tiles by 6 tiles.
                //Enter on the long wall.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 5; y++)
                            for (int x = loc[2] - 2; x < loc[2] + 4; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 5; y--)
                            for (int x = loc[2] - 2; x < loc[2] + 4; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 5; x--)
                            for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 5; x++)
                            for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber3030:
                //A square room, 6 tiles by 6 tiles.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 7; y++)
                            for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 7; y--)
                            for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 7; x--)
                            for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 7; x++)
                            for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber3040O1:
                //A rectangular room, 6 tiles by 8 tiles.
                //Enter on the small wall.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 9; y++)
                            for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 9; y--)
                            for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 9; x--)
                            for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 9; x++)
                            for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber3040O2:
                //A rectangular room, 6 tiles by 8 tiles.
                //Enter on the long wall.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 7; y++)
                            for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 7; y--)
                            for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 7; x--)
                            for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 7; x++)
                            for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber4040:
                //A square room, 8 tiles by 8 tiles.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 9; y++)
                            for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 9; y--)
                            for (int x = loc[1] - 3; x < loc[1] + 5; x++)   
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 9; x--)
                            for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 9; x++)
                            for (int y = loc[2] - 4; y < loc[2] + 4; y++)   
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber4050O1:
                //A rectangular room, 8 tiles by 10 tiles.
                //Enter on the small wall.
                //Large room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 11; y++)
                            for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 11; y--)
                            for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 11; x--)
                            for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 11; x++)
                            for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber4050O2:
                //A rectangular room, 8 tiles by 10 tiles.
                //Enter on the long wall.
                //Large room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 9; y++)
                            for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 9; y--)
                            for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 9; x--)
                            for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 9; x++)
                            for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber5080O1:
                //A rectangular room, 10 tiles by 16 tiles.
                //Enter on the small wall.
                //Large room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 17; y++)
                            for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 17; y--)
                            for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 17; x--)
                            for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 17; x++)
                            for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.Chamber5080O2:
                //A rectangular room, 10 tiles by 16 tiles.
                //Enter on the long wall.
                //Large room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 11; y++)
                            for (int x = loc[1] - 7; x < loc[1] + 9; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 11; y--)
                            for (int x = loc[1] - 7; x < loc[1] + 9; x++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 11; x--)
                            for (int y = loc[2] - 8; y < loc[2] + 8; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 11; x++)
                            for (int y = loc[2] - 8; y < loc[2] + 8; y++)
                                dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.ChamberC30:
                //A circular room, 6 tiles in diameter.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 7; y++)
                            if ((y == loc[2] + 1) || (y == loc[2] + 6))
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 7; y--)
                            if ((y == loc[2] - 1) || (y == loc[2] - 6))
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 7; x--)
                            if ((x == loc[1] - 1) || (x == loc[1] - 6))
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 7; x++)
                            if ((x == loc[1] + 1) || (x == loc[1] + 6))
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.ChamberC50:
                //A circular room, 10 tiles in diameter.
                //Large room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 11; y++)
                            if ((y == loc[2] + 1) || (y == loc[2] + 10))
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((y < loc[2] + 4) || (y > loc[2] + 7))
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 11; y--)
                            if ((y == loc[2] - 1) || (y == loc[2] - 10))
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((y > loc[2] - 4) || (y < loc[2] - 7))
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 11; x--)
                            if ((x == loc[1] - 1) || (x == loc[1] - 10))
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((x > loc[1] - 4) || (x < loc[1] - 7))
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 11; x++)
                            if ((x == loc[1] + 1) || (x == loc[1] + 10))
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((x < loc[1] + 4) || (x > loc[1] + 7))
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.ChamberO40:
                //An octagonal room, 8 tiles in diameter
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 9; y++)
                            if ((y == loc[2] + 1) || (y == loc[2] + 8))
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((y == loc[2] + 2) || (y == loc[2] + 7))
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 9; y--)
                            if ((y == loc[2] - 1) || (y == loc[2] - 8))
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((y == loc[2] - 2) || (y == loc[2] - 7))
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 9; x--)
                            if ((x == loc[1] - 1) || (x == loc[1] - 8))
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((x == loc[1] - 2) || (x == loc[1] - 7))
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 9; x++)
                            if ((x == loc[1] + 1) || (x == loc[1] + 8))
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((x == loc[1] + 2) || (x == loc[1] + 7))
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.ChamberO60:
                //An octagonal room, 12 tiles in diameter.
                //Large room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 13; y++)
                            if ((y == loc[2] + 1) || (y == loc[2] + 12))
                                for (int x = loc[1] - 2; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((y == loc[2] + 2) || (y == loc[2] + 11))
                                for (int x = loc[1] - 3; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((y == loc[2] + 3) || (y == loc[2] + 10))
                                for (int x = loc[1] - 4; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 5; x < loc[1] + 6; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 13; y--)
                            if ((y == loc[2] - 1) || (y == loc[2] - 12))
                                for (int x = loc[1] - 2; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((y == loc[2] - 2) || (y == loc[2] - 11))
                                for (int x = loc[1] - 3; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((y == loc[2] - 3) || (y == loc[2] - 10))
                                for (int x = loc[1] - 4; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[2] + 5; x < loc[1] + 6; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 13; x--)
                            if ((x == loc[1] - 1) || (x == loc[1] - 12))
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((x == loc[1] - 2) || (x == loc[1] - 11))
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((x == loc[1] - 3) || (x == loc[1] - 10))
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 13; x++)
                            if ((x == loc[1] + 1) || (x == loc[1] + 12))
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((x == loc[1] + 2) || (x == loc[1] + 11))
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if ((x == loc[1] + 3) || (x == loc[1] + 10))
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.ChamberTrap4060O1:
                //A trapezoidal room, 8 tiles wide tapering to 4 tiles wide.
                //12 tiles long.
                //Enter on the 8-tile side.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 13; y++)
                            if (y < loc[2] + 4)
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (y < loc[2] + 10)
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 13; y--)
                            if (y > loc[2] - 4)
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (y > loc[2] - 10)
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 13; x--)
                            if (x > loc[1] - 4)
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (x > loc[1] - 10)
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 13; x++)
                            if (x < loc[1] + 4)
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (x < loc[1] + 10)
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.ChamberTrap4060O2:
                //A trapezoidal room, 8 tiles wide tapering to 4 tiles wide.
                //12 tiles long.
                //Enter on the 4-tile side.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 13; y++)
                            if (y < loc[2] + 4)
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (y < loc[2] + 10)
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 13; y--)
                            if (y > loc[2] - 4)
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (y > loc[2] - 10)
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 13; x--)
                            if (x > loc[1] - 4)
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (x > loc[1] - 10)
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 13; x++)
                            if (x < loc[1] + 4)
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (x < loc[1] + 10)
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;

            case Room.ChamberTrap4060O3:
                //A Trapezoidal room, 12 tiles wide tapering to 8 tiles wide.
                //8 tiles long.
                //Enter on the 12-tile sie.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 9; y++)
                            if (y < loc[2] + 3)
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (y < loc[2] + 7)
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 9; y--)
                            if (y > loc[2] - 3)
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (y > loc[2] - 7)
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 9; x--)
                            if (x > loc[1] - 3)
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (x > loc[1] - 7)
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 9; x++)
                            if (x < loc[1] + 3)
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (x < loc[1] + 7)
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;
                
            case Room.ChamberTrap4060O4:
                //A trapezoidal room, 12 tiles wide tapering to 8 tiles wide.
                //8 tiles long.
                //Enter on the 8-tile side.
                //Small room.
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 9; y++)
                            if (y < loc[2] + 3)
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (y < loc[2] + 7)
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 9; y--)
                            if (y > loc[2] - 3)
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (y > loc[2] - 7)
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 9; x--)
                            if (x > loc[1] - 3)
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (x > loc[1] - 7)
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                    case Direction.Right:

                        for (int x = loc[1] + 1; x < loc[1] + 9; x++)
                            if (x < loc[1] + 3)
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else if (x < loc[1] + 7)
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                            else
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    dungeon[loc[0], x, y] = Map.Room;
                        break;
                }
                break;
            #endregion
            case Room.Stair:
                //Multiple floors not coded, just replace with a wall
                DeadEnd();
                break;
            case Room.BeyondPass:
                //Beyond a door, a 4-tile long, 2-tile wide passage
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 5; y++)
                        {
                            dungeon[loc[0], loc[1], y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 1, y] = Map.Passage;
                        }
                        dungeon[loc[0], loc[1], loc[2] + 5] = Map.PassageIP;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 5; y--)
                        {
                            dungeon[loc[0], loc[1], y] = Map.Passage;
                            dungeon[loc[0], loc[1] + 1, y] = Map.Passage;
                        }
                        dungeon[loc[0], loc[1], loc[2] - 5] = Map.PassageIP;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 5; x--)
                        {
                            dungeon[loc[0], x, loc[2]] = Map.Passage;
                            dungeon[loc[0], x, loc[2] - 1] = Map.Passage;
                        }
                        dungeon[loc[0], loc[1] - 5, loc[2]] = Map.PassageIP;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 5; x++)
                        {
                            dungeon[loc[0], x, loc[2]] = Map.Passage;
                            dungeon[loc[0], x, loc[2] - 1] = Map.Passage;
                        }
                        dungeon[loc[0], loc[1] + 5, loc[2]] = Map.PassageIP;
                        break;
                }
                break;
            case Room.BeyondT:
                //Beyond a door, a 2-tile wide passage, extending 2 tiles
                //Before reacing a T intersection
                switch (dir)
                {
                    case Direction.Down:
                        for (int y = loc[2] + 1; y < loc[2] + 5; y++)
                            if (y < loc[2] + 3)
                                for (int x = loc[1]; x < loc[1] + 2; x++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                        dungeon[loc[0], loc[1] - 3, loc[2] + 4] = Map.PassageIP;
                        dungeon[loc[0], loc[1] + 4, loc[2] + 4] = Map.PassageIP;
                        break;
                    case Direction.Up:
                        for (int y = loc[2] - 1; y > loc[2] - 5; y--)
                            if (y > loc[2] - 3)
                                for (int x = loc[1]; x < loc[1] + 2; x++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                        dungeon[loc[0], loc[1] - 3, loc[2] - 3] = Map.PassageIP;
                        dungeon[loc[0], loc[1] + 4, loc[2] - 3] = Map.PassageIP;
                        break;
                    case Direction.Left:
                        for (int x = loc[1] - 1; x > loc[1] - 5; x--)
                            if (x > loc[1] - 3)
                                for (int y = loc[2] - 1; y < loc[2] + 1; y++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                        dungeon[loc[0], loc[1] - 4, loc[2] - 4] = Map.PassageIP;
                        dungeon[loc[0], loc[1] - 4, loc[2] + 3] = Map.PassageIP;
                        break;
                    case Direction.Right:
                        for (int x = loc[1] + 1; x < loc[1] + 5; x++)
                            if (x < loc[1] + 3)
                                for (int y = loc[2] - 1; y < loc[2] + 1; y++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                            else
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    dungeon[loc[0], x, y] = Map.Passage;
                        dungeon[loc[0], loc[1] + 3, loc[2] - 4] = Map.PassageIP;
                        dungeon[loc[0], loc[1] + 3, loc[2] + 3] = Map.PassageIP;
                        break;
                }
                break;
        }

        //Set exits for some passages
        //Because internals don't matter, set the max size to 8
        if (size > 8)
            size = 8;

        switch (input)
        {
            case Room.Pass20DoorLt:
                //This passage has a door on the left after 4 tiles
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                            dungeon[loc[0], loc[1] + 1, loc[2] + 3] = Map.Door;
                        else
                            dungeon[loc[0], loc[1] + 1 + (size / 2), loc[2] + 3] = Map.Door;
                        break;
                    case Direction.Up:
                        if (size == 1)
                            dungeon[loc[0], loc[1] - 1, loc[2] - 3] = Map.Door;
                        else
                            dungeon[loc[0], loc[1] - (size / 2), loc[2] - 3] = Map.Door;
                        break;
                    case Direction.Left:
                        if (size == 1)
                            dungeon[loc[0], loc[1] - 3, loc[2] + 1] = Map.Door;
                        else
                            dungeon[loc[0], loc[1] - 3, loc[2] + (size / 2)] = Map.Door;
                        break;
                    case Direction.Right:
                        if (size == 1)
                            dungeon[loc[0], loc[1] + 3, loc[2] - 1] = Map.Door;
                        else
                           dungeon[loc[0], loc[1] + 3, loc[2] - 1 - (size / 2)] = Map.Door;
                        break;
                }
                break;
            case Room.Pass20DoorRt:
                //This passage has a door on the right after 4 tiles
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                            dungeon[loc[0], loc[1] - 1, loc[2] + 3] = Map.Door;
                        else
                            dungeon[loc[0], loc[1] - (size / 2), loc[2] + 3] = Map.Door;
                        break;
                    case Direction.Up:
                        if (size == 1)
                            dungeon[loc[0], loc[1] + 1, loc[2] - 3] = Map.Door;
                        else
                            dungeon[loc[0], loc[1] + (size / 2), loc[2] - 3] = Map.Door;
                        break;
                    case Direction.Left:
                        if (size == 1)
                            dungeon[loc[0], loc[1] - 3, loc[2] - 1] = Map.Door;
                        else
                            dungeon[loc[0], loc[1] - 3, loc[2] - (size / 2) - 1] = Map.Door;
                        break;
                    case Direction.Right:
                        if (size == 1)
                            dungeon[loc[0], loc[1] + 3, loc[2] + 1] = Map.Door;
                        else
                            dungeon[loc[0], loc[1] + 3, loc[2] + (size / 2)] = Map.Door;
                        break;
                }
                break;
            case Room.Pass20PassLt:
                //This passage has a passage on the left after 4 tiles
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                            dungeon[loc[0], loc[1] + 1, loc[2] + 3] = Map.PassageIP;
                        else
                            dungeon[loc[0], loc[1] + 1 + (size / 2), loc[2] + 3] = Map.PassageIP;
                        break;
                    case Direction.Up:
                        if (size == 1)
                            dungeon[loc[0], loc[1] - 1, loc[2] - 3] = Map.PassageIP;
                        else
                            dungeon[loc[0], loc[1] - (size / 2), loc[2] - 3] = Map.PassageIP;
                        break;
                    case Direction.Left:
                        if (size == 1)
                            dungeon[loc[0], loc[1] - 3, loc[2] + 1] = Map.PassageIP;
                        else
                            dungeon[loc[0], loc[1] - 3, loc[2] + (size / 2)] = Map.PassageIP;
                        break;
                    case Direction.Right:
                        if (size == 1)
                            dungeon[loc[0], loc[1] + 3, loc[2] - 1] = Map.PassageIP;
                        else
                            dungeon[loc[0], loc[1] + 3, loc[2] - 1 - (size / 2)] = Map.PassageIP;
                        break;
                }
                break;
            case Room.Pass20PassRt:
                //This passage has a passage on the right after 4 tiles
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                            dungeon[loc[0], loc[1] - 1, loc[2] + 3] = Map.PassageIP;
                        else
                            dungeon[loc[0], loc[1] - (size / 2), loc[2] + 3] = Map.PassageIP;
                        break;
                    case Direction.Up:
                        if (size == 1)
                            dungeon[loc[0], loc[1] + 1, loc[2] - 3] = Map.PassageIP;
                        else
                            dungeon[loc[0], loc[1] + (size / 2), loc[2] - 3] = Map.PassageIP;
                        break;
                    case Direction.Left:
                        if (size == 1)
                            dungeon[loc[0], loc[1] - 3, loc[2] - 1] = Map.PassageIP;
                        else
                            dungeon[loc[0], loc[1] - 3, loc[2] - (size / 2) - 1] = Map.PassageIP;
                        break;
                    case Direction.Right:
                        if (size == 1)
                            dungeon[loc[0], loc[1] + 3, loc[2] + 1] = Map.PassageIP;
                        else
                            dungeon[loc[0], loc[1] + 3, loc[2] + (size / 2)] = Map.PassageIP;
                        break;
                }
                break;
            case Room.Pass20Door:
                //This passage always ends in a door
                switch (dir)
                {
                    case Direction.Down:
                        dungeon[loc[0], loc[1], loc[2] + 4] = Map.Door;
                        break;
                    case Direction.Up:
                        dungeon[loc[0], loc[1], loc[2] - 4] = Map.Door;
                        break;
                    case Direction.Left:
                        dungeon[loc[0], loc[1] - 4, loc[2]] = Map.Door;
                        break;
                    case Direction.Right:
                        dungeon[loc[0], loc[1] + 4, loc[2]] = Map.Door;
                        break;
                }
                break;
            case Room.Pass20Dead:
                //This passage has a 10% chance of having a secret door at the end
                switch (dir)
                {
                    case Direction.Down:
                        if (Random.Range(1,11) == 1)
                            dungeon[loc[0], loc[1], loc[2] + 4] = Map.SecretDoorStart;
                        break;
                    case Direction.Up:
                        if (Random.Range(1, 11) == 1)
                            dungeon[loc[0], loc[1], loc[2] - 4] = Map.SecretDoorStart;
                        break;
                    case Direction.Left:
                        if (Random.Range(1, 11) == 1)
                            dungeon[loc[0], loc[1] - 4, loc[2]] = Map.SecretDoorStart;
                        break;
                    case Direction.Right:
                        if (Random.Range(1, 11) == 1)
                            dungeon[loc[0], loc[1] + 4, loc[2]] = Map.SecretDoorStart;
                        break;
                }
                break;
        }
    }

    bool Incomplete()
    {
        //Starting at the bottom floor and going up,
        //Start at the left side of the floor and go right
        //Start at the top of the row of tiles and go down
        //If a PassageIP or Door is found, the dungeon is incomplete
        //If no floors are incomplete, the dungeon is complete
        for (int f = 0; f < numFloors; f++)
            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapDepth; y++)
                {
                    if (dungeon[f, x, y] == Map.PassageIP)
                    {
                        Debug.Log("Found incomplete dungeon: PassageIP at " + f + " " + x + " " + y);
                        loc = new int[] { f, x, y };
                        return true;
                    }
                    if ((dungeon[f, x, y] == Map.Door) || (dungeon[f, x, y] == Map.SecretDoorStart))
                    {
                        Debug.Log("Found incomplete dungeon: " + dungeon[f,x,y] + " at " + f + " " + x + " " + y);
                        loc = new int[] { f, x, y };
                        if (dungeon[f, x, y] == Map.SecretDoorStart)
                            dungeon[f, x, y] = Map.SecretDoor;
                        return true;
                    }
                }
        return false;
    }

    void RollPassage()
    {
        //To insure that a given location is repeated, unset PassageIP
        dungeon[loc[0], loc[1], loc[2]] = Map.Passage;

        Direction dir;
        Room passage;
        int maxSize = 2;
        int temp1 = 0;
        int temp2 = 0;
        int size = 0;
        bool valid = false;

        //Only trying a set number of times before setting a dead end
        int attempts = 1;

        //Determine the direction of a passage:
        //If the tile to the right is set, direction is left
        //If the tile to the left is set, direction is right
        //If the tile above is set, the direction is down
        //If the tile below is set, the diretion is up
        //If all the surrounding tiles are set, there is nowhere to go!
        if (
            ((dungeon[loc[0], loc[1] - 1, loc[2]] == Map.Room) || 
             (dungeon[loc[0], loc[1] - 1, loc[2]] == Map.Passage) ||
             (dungeon[loc[0], loc[1] - 1, loc[2]] == Map.Pillar)) &&
            ((dungeon[loc[0], loc[1] + 1, loc[2]] == Map.Room) ||
             (dungeon[loc[0], loc[1] + 1, loc[2]] == Map.Passage) ||
             (dungeon[loc[0], loc[1] + 1, loc[2]] == Map.Pillar)) &&
            ((dungeon[loc[0], loc[1], loc[2] - 1] == Map.Room) ||
             (dungeon[loc[0], loc[1], loc[2] - 1] == Map.Passage) ||
             (dungeon[loc[0], loc[1], loc[2] - 1] == Map.Pillar)) &&
            ((dungeon[loc[0], loc[1], loc[2] + 1] == Map.Room) ||
             (dungeon[loc[0], loc[1], loc[2] + 1] == Map.Passage) ||
             (dungeon[loc[0], loc[1], loc[2] + 1] == Map.Pillar))
            )
        {
            return;
        }
        else
        {
            if ((dungeon[0, loc[1] + 1, loc[2]] == Map.Room) || (dungeon[0, loc[1] + 1, loc[2]] == Map.Passage) || (dungeon[0, loc[1] + 1, loc[2]] == Map.Pillar))
                dir = Direction.Left;
            else if ((dungeon[0, loc[1] - 1, loc[2]] == Map.Room) || (dungeon[0, loc[1] - 1, loc[2]] == Map.Passage) || (dungeon[0, loc[1] - 1, loc[2]] == Map.Pillar))
                dir = Direction.Right;
            else if ((dungeon[0, loc[1], loc[2] + 1] == Map.Room) || (dungeon[0, loc[1], loc[2] + 1] == Map.Passage) || (dungeon[0, loc[1], loc[2] + 1] == Map.Pillar))
                dir = Direction.Up;
            else
                dir = Direction.Down;
        }

        //If a passage is invalid, retry from here
        RETRYPASSAGE:
        maxSize = 2;
        temp1 = 0;
        temp2 = 0;
        size = 0;

        //From a weighted list, determine a random passage shape
        switch (Random.Range(1, 21))
        {
            case 1:
            case 2:
                passage = Room.StndPass;
                break;
            case 3:
                passage = Room.Pass20DoorRt;
                break;
            case 4:
                passage = Room.Pass20DoorLt;
                break;
            case 5:
                passage = Room.Pass20Dead;
                break;
            case 6:
            case 7:
                passage = Room.Pass20PassRt;
                break;
            case 8:
            case 9:
                passage = Room.Pass20PassLt;
                break;
            case 10:
                passage = Room.Pass20Dead;
                break;
            case 11:
            case 12:
                passage = Room.Pass20L;
                break;
            case 13:
            case 14:
                passage = Room.Pass20R;
                break;
            case 20:
                passage = Room.Stair;
                break;
            default:
                passage = Room.Chamber;
                break;
        }

        //If a rolled passage is actually a passage,
        //Determine the maximum width it can be

        //If it's from a room, it cannot be wider than two tiles smaller
        //Than the smallest dimension of the room

        //If it's from a passage, it cannot be wider than two tiles

        if ((passage != Room.Stair) && (passage != Room.Chamber))
        {
            switch (dir)
            {
                case Direction.Left:
                    if (dungeon[loc[0], loc[1] + 1, loc[2]] == Map.Passage)
                        break;
                    for (int x = loc[1] + 1; (dungeon[loc[0], x, loc[2]] == Map.Room) || (dungeon[loc[0], x, loc[2]] == Map.Well) || (dungeon[loc[0], x, loc[2]] == Map.Pillar); x++)
                        temp1++;
                    for (int y = loc[2]; (dungeon[loc[0], loc[1] + 1, y] == Map.Room) || (dungeon[loc[0], loc[1] + 1, y] == Map.Well) || (dungeon[loc[0], loc[1] + 1, y] == Map.Pillar); y++)
                        temp2++;
                    for (int y = loc[2] - 1; (dungeon[loc[0], loc[1] + 1, y] == Map.Room) || (dungeon[loc[0], loc[1] + 1, y] == Map.Well) || (dungeon[loc[0], loc[1] + 1, y] == Map.Pillar); y--)
                        temp2++;
                    maxSize = ((int)Mathf.Min(new float[] { temp1, temp2 })) - 2;
                    break;
                case Direction.Right:
                    if (dungeon[loc[0], loc[1] - 1, loc[2]] == Map.Passage)
                        break;
                    for (int x = loc[1] - 1; (dungeon[loc[0], x, loc[2]] == Map.Room) || (dungeon[loc[0], x, loc[2]] == Map.Well) || (dungeon[loc[0], x, loc[2]] == Map.Pillar); x--)
                        temp1++;
                    for (int y = loc[2]; (dungeon[loc[0], loc[1] - 1, y] == Map.Room) || (dungeon[loc[0], loc[1] - 1, y] == Map.Well) || (dungeon[loc[0], loc[1] - 1, y] == Map.Pillar); y++)
                        temp2++;
                    for (int y = loc[2] - 1; (dungeon[loc[0], loc[1] - 1, y] == Map.Room) || (dungeon[loc[0], loc[1] - 1, y] == Map.Well) || (dungeon[loc[0], loc[1] - 1, y] == Map.Pillar); y--)
                        temp2++;
                    maxSize = ((int)Mathf.Min(new float[] { temp1, temp2 })) - 2;
                    break;
                case Direction.Up:
                    if (dungeon[loc[0], loc[1], loc[2] + 1] == Map.Passage)
                        break;
                    for (int y = loc[2] + 1; (dungeon[loc[0], loc[1], y] == Map.Room) || (dungeon[loc[0], loc[1], y] == Map.Well) || (dungeon[loc[0], loc[1], y] == Map.Pillar); y++)
                        temp1++;
                    for (int x = loc[1]; (dungeon[loc[0], x, loc[2] + 1] == Map.Room) || (dungeon[loc[0], x, loc[2] + 1] == Map.Well) || (dungeon[loc[0], x, loc[2] + 1] == Map.Pillar); x++)
                        temp2++;
                    for (int x = loc[1] - 1; (dungeon[loc[0], x, loc[2] + 1] == Map.Room) || (dungeon[loc[0], x, loc[2] + 1] == Map.Well) || (dungeon[loc[0], x, loc[2] + 1] == Map.Pillar); x--)
                        temp2++;
                    maxSize = ((int)Mathf.Min(new float[] { temp1, temp2 })) - 2;
                    break;
                case Direction.Down:
                    if (dungeon[loc[0], loc[1], loc[2] - 1] == Map.Passage)
                        break;
                    for (int y = loc[2] - 1; (dungeon[loc[0], loc[1], y] == Map.Room) || (dungeon[loc[0], loc[1], y] == Map.Well) || (dungeon[loc[0], loc[1], y] == Map.Pillar); y--)
                        temp1++;
                    for (int x = loc[1]; (dungeon[loc[0], x, loc[2] - 1] == Map.Room) || (dungeon[loc[0], x, loc[2] - 1] == Map.Well) || (dungeon[loc[0], x, loc[2] - 1] == Map.Pillar); x++)
                        temp2++;
                    for (int x = loc[1] - 1; (dungeon[loc[0], x, loc[2] - 1] == Map.Room) || (dungeon[loc[0], x, loc[2] - 1] == Map.Well) || (dungeon[loc[0], x, loc[2] - 1] == Map.Pillar); x--)
                        temp2++;
                    maxSize = ((int)Mathf.Min(new float[] { temp1, temp2 })) - 2;
                    break;
            }

            //From a weighted list, determine how wide the passage actually is
            if (maxSize == 2)
                switch (Random.Range(1, 13))
                {
                    case 1:
                    case 2:
                        size = 1;
                        break;
                    default:
                        size = 2;
                        break;
                }
            else
                switch (Random.Range(1, 21))
                {
                    case 1:
                    case 2:
                        size = 1;
                        break;
                    case 13:
                    case 14:
                        size = 4;
                        break;
                    case 15:
                    case 16:
                        size = 6;
                        break;
                    case 17:
                        size = 8;
                        break;
                    case 18:
                        size = 9;
                        break;
                    case 19:
                        size = 10;
                        break;
                    case 20:
                        size = 11;
                        break;
                    default:
                        size = 2;
                        break;
                }

            //If a rolled size is larger than the max size, fix!
            if (size > maxSize)
                size = maxSize;
        }

        //If a rolled passage is actually a chamber, determine which one
        if (passage == Room.Chamber)
            passage = RollChamber();

        //Validate the passage (or chamber/stair, as the case may be)
        valid = IsValid(ref passage, dir, size);

        //If a passage isn't valid and there are fewer than 100 attempts,
        //Try again
        //If the passage is valid, set it
        //If after 100 attempts no valid passage is generated, set a dead end
        if ((!valid) && (attempts <= 100))
        {
            attempts++;
            goto RETRYPASSAGE;
        }
        else if (valid)
            SetRoom(passage, size, dir);
        else
        {
            Debug.Log("Too many attempts to fit, setting dead end");
            DeadEnd();
        }
    }

    void BeyondDoor()
    {
        //If a door has not already been set, determine which door it is
        if (dungeon[loc[0], loc[1], loc[2]] == Map.Door)
            dungeon[loc[0], loc[1], loc[2]] = RollDoor();

        Direction dir;
        Room beyond = Room.Stair;
        bool valid = false;
        int attempts = 1;

        //Determine the direction the door is facing:
        //If the tile to the right is set, direction is left
        //If the tile to the left is set, direction is right
        //If the tile above is set, the direction is down
        //If the tile below is set, the direction is up
        //If all the surrounding tiles are set, there is nowhere to go!
        if (
            ((dungeon[loc[0], loc[1] - 1, loc[2]] == Map.Room) ||
             (dungeon[loc[0], loc[1] - 1, loc[2]] == Map.Passage) ||
             (dungeon[loc[0], loc[1] - 1, loc[2]] == Map.Pillar)) &&
            ((dungeon[loc[0], loc[1] + 1, loc[2]] == Map.Room) ||
             (dungeon[loc[0], loc[1] + 1, loc[2]] == Map.Passage) ||
             (dungeon[loc[0], loc[1] + 1, loc[2]] == Map.Pillar)) &&
            ((dungeon[loc[0], loc[1], loc[2] - 1] == Map.Room) ||
             (dungeon[loc[0], loc[1], loc[2] - 1] == Map.Passage) ||
             (dungeon[loc[0], loc[1], loc[2] - 1] == Map.Pillar)) &&
            ((dungeon[loc[0], loc[1], loc[2] + 1] == Map.Room) ||
             (dungeon[loc[0], loc[1], loc[2] + 1] == Map.Passage) ||
             (dungeon[loc[0], loc[1], loc[2] + 1] == Map.Pillar))
            )
        {
            return;
        }
        else
        {
            if ((dungeon[0, loc[1] + 1, loc[2]] == Map.Room) || (dungeon[0, loc[1] + 1, loc[2]] == Map.Passage) || (dungeon[0, loc[1] + 1, loc[2]] == Map.Pillar))
                dir = Direction.Left;
            else if ((dungeon[0, loc[1] - 1, loc[2]] == Map.Room) || (dungeon[0, loc[1] - 1, loc[2]] == Map.Passage) || (dungeon[0, loc[1] - 1, loc[2]] == Map.Pillar))
                dir = Direction.Right;
            else if ((dungeon[0, loc[1], loc[2] + 1] == Map.Room) || (dungeon[0, loc[1], loc[2] + 1] == Map.Passage) || (dungeon[0, loc[1], loc[2] + 1] == Map.Pillar))
                dir = Direction.Up;
            else
                dir = Direction.Down;
        }

        //If a given scenario does not work, try again from here
        RETRYDOOR:

        switch (Random.Range(1, 21))
        {
            case 1:
            case 2:
                //Beyond the door is a T intersection passage
                beyond = Room.BeyondT;
                break;
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
                //Beyond the door is a straight passage
                beyond = Room.BeyondPass;
                break;
            case 19:
                //Beyond the door is a staircase
                beyond = Room.Stair;
                break;
            case 20:
                //The door is fake
                dungeon[loc[0], loc[1], loc[2]] = Map.FalseDoor;
                break;
            default:
                //Beyond the door is a chamber
                beyond = RollChamber();
                break;
        }

        //If there is actually something behind the door, validate it
        //Otherwise, set
        if ((dungeon[loc[0], loc[1], loc[2]] != Map.FalseDoor) && (beyond != Room.Stair))
        {
            valid = (IsValid(ref beyond, dir, 0));

            //Try 100 times to get something to work
            if ((!valid) && (attempts <= 100))
            {
                attempts++;
                goto RETRYDOOR;
            }
            else if (valid)
            {
                SetRoom(beyond, 0, dir);
            }
            else
            {
                Debug.Log("Too many attempts to fit, setting dead end");
                DeadEnd();
            }
        }
        else
            SetRoom(beyond, 0, dir);
    }

    Room RollChamber()
    {
        //From a weighted list, determine which chamber to place
        //Depending on the size, set a number of exits from the room

        switch (Random.Range(1, 21))
        {
            case 3:
            case 4:
                NormExits();
                return Room.Chamber3030;
            case 5:
            case 6:
                NormExits();
                return Room.Chamber4040;
            case 7:
            case 8:
            case 9:
                NormExits();
                return Room.Chamber2030O1;
            case 10:
            case 11:
            case 12:
                NormExits();
                return Room.Chamber3040O1;
            case 13:
            case 14:
                LargeExits();
                return Room.Chamber4050O1;
            case 15:
                LargeExits();
                return Room.Chamber5080O1;
            case 16:
                NormExits();
                return Room.ChamberC30;
            case 17:
                LargeExits();
                return Room.ChamberC50;
            case 18:
                NormExits();
                return Room.ChamberO40;
            case 19:
                LargeExits();
                return Room.ChamberO60;
            case 20:
                LargeExits();
                return Room.ChamberTrap4060O1;
            default:
                NormExits();
                return Room.Chamber2020;
        }
    }
    void NormExits()
    {
        //Small room, so no more than 4 exits
        switch (Random.Range(1, 21))
        {
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
                numExits = 1;
                break;
            case 12:
            case 13:
            case 14:
            case 15:
                numExits = 2;
                break;
            case 16:
            case 17:
            case 18:
                numExits = 3;
                break;
            case 19:
            case 20:
                numExits = 4;
                break;
            default:
                numExits = 0;
                break;
        }
    }
    void LargeExits()
    {
        //Large room, so no more than 6 exits
        switch (Random.Range(1, 21))
        {
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
                numExits = 1;
                break;
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
                numExits = 2;
                break;
            case 14:
            case 15:
            case 16:
            case 17:
                numExits = 3;
                break;
            case 18:
                numExits = 4;
                break;
            case 19:
                numExits = 5;
                break;
            case 20:
                numExits = 6;
                break;
            default:
                numExits = 0;
                break;
        }
    }

    bool IsValid(ref Room id, Direction dir, int size)
    {
        //Validation method
        //For each testing case, first check to see if any of the required tiles
        //Would be out of the range of the map. Check three directions:
        //If going Down, check left, right, and down.
        //If going Up, check left, right, and up.
        //If going Left, check up, down, and left.
        //If going Right, check up, down, and right.
        //If the room or passage can fit in the map, determine if the space is empty
        //If there is something there already, placement is invalid
        //Check not only the actual tiles the room would cover,
        //But also the surrounding tiles -- to have walls between rooms and passages


        Debug.Log("Checking " + id + " at " + loc[0] + " " + loc[1] + " " + loc[2] + " going " + dir + " " + " with size " + size);

        //Because internals don't matter, all large passages can be treated the same
        if (size > 8)
            size = 8;

        int check = (size / 2);

        //Some rooms have multiple possible orientations. At least one must be valid
        bool orient1 = true;
        bool orient2 = true;
        bool orient3 = true;
        bool orient4 = true;

        switch (id)
        {
            #region Passages
            case Room.StndPass:
            case Room.Pass20DoorLt:
            case Room.Pass20PassLt:
            case Room.Pass20DoorRt:
            case Room.Pass20PassRt:
                //Because these passages all have the same shape and only differ
                //In their branches, they can be validated the same way.
                //They are straight passages 6 tiles long
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                        {
                            if ((loc[1] - 1 < 0) || (loc[1] + 1 >= mapWidth) || (loc[2] + 6 >= mapDepth))
                                return false;
                            for (int y = loc[2]; y < loc[2] + 7; y++)
                                for (int x = loc[1] - 1; x < loc[1] + 2; x++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[1] - check < 0) || (loc[1] + check + 1 >= mapWidth) || (loc[2] + 6 >= mapDepth))
                                return false;
                            for (int y = loc[2]; y < loc[2] + 7; y++)
                                for (int x = loc[1] - check; x < loc[1] + check + 2; x++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                    case Direction.Up:
                        if (size == 1)
                        {
                            if ((loc[1] - 1 < 0) || (loc[1] + 1 >= mapWidth) || (loc[2] - 6 < 0))
                                return false;
                            for (int y = loc[2]; y > loc[2] - 7; y--)
                                for (int x = loc[1] - 1; x < loc[1] + 2; x++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[1] - check < 0) || (loc[1] + check + 1 >= mapWidth) || (loc[2] - 6 < 0))
                                return false;
                            for (int y = loc[2]; y > loc[2] - 7; y--)
                                for (int x = loc[1] - check; x < loc[1] + check + 2; x++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                    case Direction.Left:
                        if (size == 1)
                        {
                            if ((loc[2] - 1 < 0) || (loc[2] + 1 >= mapDepth) || (loc[1] - 6 < 0))
                                return false;
                            for (int x = loc[1]; x > loc[1] - 7; x--)
                                for (int y = loc[2] - 1; y < loc[2] + 2; y++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[2] - check - 1 < 0) || (loc[2] + check >= mapDepth) || (loc[1] - 6 < 0))
                                return false;
                            for (int x = loc[1]; x > loc[1] - 7; x--)
                                for (int y = loc[2] - check - 1; y < loc[2] + check + 1; y++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                    case Direction.Right:
                        if (size == 1)
                        {
                            if ((loc[2] - 1 < 0) || (loc[2] + 1 >= mapDepth) || (loc[1] + 6 >= mapWidth))
                                return false;
                            for (int x = loc[1]; x < loc[1] + 7; x++)
                                for (int y = loc[2] - 1; y < loc[2] + 2; y++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[2] - check - 1 < 0) || (loc[2] + check >= mapDepth) || (loc[1] + 6 >= mapWidth))
                                return false;
                            for (int x = loc[1]; x < loc[1] + 7; x++)
                                for (int y = loc[2] - check - 1; y < loc[2] + check + 1; y++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                }
                break;
            case Room.Pass20Door:   
            case Room.Pass20Dead:
                //Because these passages have the same shape and only differ
                //In how they end, they can be validated the same way.
                //They are straight passages 4 tiles long
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                        {
                            if ((loc[1] - 1 < 0) || (loc[1] + 1 >= mapWidth) || (loc[2] + 4 >= mapDepth))
                                return false;
                            for (int y = loc[2]; y < loc[2] + 5; y++)
                                for (int x = loc[1] - 1; x < loc[1] + 2; x++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[1] - check < 0) || (loc[1] + check + 1 >= mapWidth) || (loc[2] + 4 >= mapDepth))
                                return false;
                            for (int y = loc[2]; y < loc[2] + 5; y++)
                                for (int x = loc[1] - check; x < loc[1] + check + 2; x++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                    case Direction.Up:
                        if (size == 1)
                        {
                            if ((loc[1] - 1 < 0) || (loc[1] + 1 >= mapWidth) || (loc[2] - 4 < 0))
                                return false;
                            for (int y = loc[2]; y > loc[2] - 5; y--)
                                for (int x = loc[1] - 1; x < loc[1] + 2; x++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[1] - check < 0) || (loc[1] + check + 1 >= mapWidth) || (loc[2] - 4 < 0))
                                return false;
                            for (int y = loc[2]; y > loc[2] - 5; y--)
                                for (int x = loc[1] - check; x < loc[1] + check + 2; x++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                    case Direction.Left:
                        if (size == 1)
                        {
                            if ((loc[2] - 1 < 0) || (loc[2] + 1 >= mapDepth) || (loc[1] - 4 < 0))
                                return false;
                            for (int x = loc[1]; x > loc[1] - 5; x--)
                                for (int y = loc[2] - 1; y < loc[2] + 2; y++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[2] - check - 1 < 0) || (loc[2] + check >= mapDepth) || (loc[1] - 4 < 0))
                                return false;
                            for (int x = loc[1]; x > loc[1] - 5; x--)
                                for (int y = loc[2] - check - 1; y < loc[2] + check + 1; y++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                    case Direction.Right:
                        if (size == 1)
                        {
                            if ((loc[2] - 1 < 0) || (loc[2] + 1 >= mapDepth) || (loc[1] + 4 >= mapWidth))
                                return false;
                            for (int x = loc[1]; x < loc[1] + 5; x++)
                                for (int y = loc[2] - 1; y < loc[2] + 2; y++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[2] - check - 1 < 0) || (loc[2] + check + 1>= mapDepth) || (loc[1] + 4 >= mapWidth))
                                return false;
                            for (int x = loc[1]; x < loc[1] + 5; x++)
                                for (int y = loc[2] - check - 1; y < loc[2] + check + 1; y++)
                                    if ((x == loc[1]) && (y == loc[2]))
                                    {
                                        //This is the host tile. Ignore it when validating
                                    }
                                    else if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                }
                break;
            case Room.Pass20L:
                //This passage continues straight for 4 tiles, then turns left
                //And continues for 2 tiles. The width of the passage after
                //The turn is the same as the width before the turn
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                        {
                            if ((loc[1] - 1 < 0) || (loc[1] + 3 >= mapWidth) || (loc[2] + 5 >= mapDepth))
                                return false;
                            for (int y = loc[2]; y < loc[2] + 6; y++)
                                if (y < loc[2] + 3)
                                {
                                    for (int x = loc[1] - 1; x < loc[1] + 2; x++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int x = loc[1] - 1; x < loc[1] + 4; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                        
                        }
                        else
                        {
                            if ((loc[1] - check < 0) || (loc[1] + check + 3 >= mapWidth) || (loc[2] + size + 4 >= mapDepth))
                                return false;
                            for (int y = loc[2]; y < loc[2] + size + 4; y++)
                                if (y < loc[2] + 3)
                                {
                                    for (int x = loc[1] - check; x < loc[1] + check + 1; x++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else 
                                    for (int x = loc[1] - check; x < loc[1] + check + 4; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        break;
                    case Direction.Up:
                        if (size == 1)
                        {
                            if ((loc[1] - 3 < 0) || (loc[1] + 1 >= mapWidth) || (loc[2] - 5 < 0))
                                return false;
                            for (int y = loc[2]; y > loc[2] + 6; y++)
                                if (y > loc[2] + 3)
                                {
                                    for (int x = loc[1] - 1; x < loc[1] + 2; x++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 2; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        else
                        {
                            if ((loc[1] - check - 2 < 0) || (loc[1] + check  + 1 >= mapWidth) || (loc[2] - size - 4 < 0))
                                return false;
                            for (int y = loc[2]; y > loc[2] - size - 5; y--)
                                if (y > loc[2] - 3)
                                {
                                    for (int x = loc[1] - check; x < loc[1] + check + 2; x++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int x = loc[1] - check - 2; x < loc[1] + check + 2; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        break;
                    case Direction.Left:
                        if (size == 1)
                        {
                            if ((loc[2] - 1 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] - 5 < 0))
                                return false;
                            for (int x = loc[1]; x > loc[1] - 6; x--)
                                if (x > loc[1] - 3)
                                {
                                    for (int y = loc[2] - 1; y < loc[2] + 2; y++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int y = loc[2] - 1; y < loc[2] + 4; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[2] - check - 1 < 0) || (loc[2] + check + 2 >= mapDepth) || (loc[1] - size - 4 < 0))
                                return false;
                            for (int x = loc[1]; x > loc[1] - size - 5; x--)
                                if (x > loc[1] - 3)
                                {
                                    for (int y = loc[2] - check - 1; y < loc[2] + check; y++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int y = loc[2] - check - 1; y < loc[2] + check + 2; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        break;
                    case Direction.Right:
                        if (size == 1)
                        {
                            if ((loc[2] - 3 < 0) || (loc[2] + 1 >= mapDepth) || (loc[1] + 5 >= mapWidth))
                                return false;
                            for (int x = loc[1]; x < loc[1] + 6; x++)
                                if (x < loc[1] + 3)
                                {
                                    for (int y = loc[2] - 1; y < loc[2] + 2; y++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int y = loc[2] - 3; y < loc[2] + 2; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        else
                        {
                            if ((loc[2] - check - 3 < 0) || (loc[2] + check >= mapDepth) || (loc[1] + size + 4 >= mapWidth))
                                return false;
                            for (int x = loc[1]; x < loc[1] + size + 5; x++)
                                if (x < loc[1] + 3)
                                {
                                    for (int y = loc[2] - check - 1; y < loc[2] + check + 1; y++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int y = loc[2] - check - 3; y < loc[2] + check + 1; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        break;
                }
                break;
            case Room.Pass20R:
                //This passage continues straight for 4 tiles, then turns right
                //And continues for 2 tiles. The width of the passage after
                //The turn is the same as the width before the turn
                switch (dir)
                {
                    case Direction.Down:
                        if (size == 1)
                        {
                            if ((loc[1] - 3 < 0) || (loc[1] + 1 >= mapWidth) || (loc[2] + 5 >= mapDepth))
                                return false;
                            for (int y = loc[2]; y < loc[2] + 6; y++)
                                if (y < loc[2] + 3)
                                {
                                    for (int x = loc[1] - 1; x < loc[1] + 2; x++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int x = loc[1] - 3; x < loc[1] + 2; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        else
                        {
                            if ((loc[1] - check - 2 < 0) || (loc[1] + check + 1 >= mapWidth) || (loc[2] + size + 4 >= mapDepth))
                                return false;
                            for (int y = loc[2]; y < loc[2] + size + 5; y++)
                                if (y < loc[2] + 3)
                                {
                                    for (int x = loc[1] - check; x < loc[1] + check + 2; x++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else 
                                    for (int x = loc[1] - check - 2; x < loc[1] + check + 2; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        break;
                    case Direction.Up:
                        if (size == 1)
                        {
                            if ((loc[1] - 1 < 0) || (loc[1] + 3 >= mapWidth) || (loc[2] - 5 < 0))
                                return false;
                            for (int y = loc[2]; y > loc[2] - 6; y--)
                                if (y > loc[2] - 3)
                                {
                                    for (int x = loc[1] - 1; x < loc[1] + 2; x++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int x = loc[1] - 1; x < loc[1] + 4; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        else
                        {
                            if ((loc[1] - check < 0) || (loc[1] + check + 3 >= mapWidth) || (loc[2] - size - 4 < 0))
                                return false;
                            for (int y = loc[2]; y > loc[2] - size - 5; y--)
                                if (y > loc[2] - 3)
                                {
                                    for (int x = loc[1] - check; x < loc[1] + check + 1; x++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else 
                                    for (int x = loc[1] - check; x < loc[1] + check + 3; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        break;
                    case Direction.Left:
                        if (size == 1)
                        {
                            if ((loc[2] - 3 < 0) || (loc[2] + 1 >= mapDepth) || (loc[1] - 5 < 0))
                                return false;
                            for (int x = loc[1]; x > loc[1] - 6; x--)
                                if (x > loc[1] - 3)
                                {
                                    for (int y = loc[2] - 1; y < loc[2] + 2; y++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else 
                                    for (int y = loc[2] - 3; y < loc[2] + 2; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        else
                        {
                            if ((loc[2] - check - 3 < 0) || (loc[2] + check >= mapDepth) || (loc[1] - size - 4 < 0))
                                return false;
                            for (int x = loc[1]; x > loc[1] - size - 4; x++)
                                if (x > loc[1] - 3)
                                {
                                    for (int y = loc[2] - check - 1; y < loc[2] + check + 1; y++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else 
                                    for (int y = loc[2] - check - 3; y < loc[2] + check + 1; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        break;
                    case Direction.Right:
                        if (size == 1)
                        {
                            if ((loc[2] - 1 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] + 5 >= mapWidth))
                                return false;
                            for (int x = loc[1]; x < loc[1] + 6; x++)
                                if (x < loc[1] + 3)
                                {
                                    for (int y = loc[2] - 1; y < loc[2] + 2; y++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int y = loc[2] - 1; y < loc[2] + 4; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        else
                        {
                            if ((loc[2] - check - 1 < 0) || (loc[2] + check + 2 >= mapDepth) || (loc[1] + size + 4 >= mapWidth))
                                return false;
                            for (int x = loc[1]; x < loc[1] + size + 5; x++)
                                if (x < loc[1] + 3)
                                {
                                    for (int y = loc[2] - check - 1; y < loc[2] + check + 1; y++)
                                        if ((x == loc[1]) && (y == loc[2]))
                                        {
                                            //This is the host tile. Ignore it when validating
                                        }
                                        else if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                                }
                                else
                                    for (int y = loc[2] - check - 1; y < loc[2] + check + 3; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                            return false;
                        }
                        break;
                }
                break;
            #endregion
            #region Chambers
            case Room.Chamber2020:
                //This room is 4 tiles by 4 tiles
                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 2 < 0) || (loc[1] + 3 >= mapWidth) || (loc[2] + 5 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 6; y++)
                            for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Up:
                        if ((loc[1] - 2 < 0) || (loc[1] + 3 >= mapWidth) || (loc[2] - 5 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 6; y--)
                            for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Left:
                        if ((loc[2] - 3 < 0) || (loc[2] + 2 >= mapDepth) || (loc[1] - 5 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 6; x--)
                            for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Right:
                        if ((loc[2] - 3 < 0) || (loc[2] + 2 >= mapDepth) || (loc[1] + 5 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[2] + 6; x++)
                            for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                }
                break;

            case Room.Chamber2030O1:
                //This room is 4 tiles by 6 tiles
                //First, test entering on the small wall.
                //If that orientation is invalid, test entering on the long wall
                //If that orientation is valid, re-set the room to the proper ID
                //If both orientations are invalid, the room is invalid

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 2 < 0) || (loc[1] + 3 >= mapWidth) || (loc[2] + 7 >= mapDepth))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] + 1; y < loc[2] + 8; y++)
                            {
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] + 5 >= mapDepth))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] + 1; y < loc[2] + 6; y++)
                                {
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }
                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber2030O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Up:
                        if ((loc[1] - 2 < 0) || (loc[1] + 3 >= mapWidth) || (loc[2] - 7 < 0))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] - 1; y > loc[2] - 8; y--)
                            {
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] - 5 < 0))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] - 1; y > loc[2] - 6; y--)
                                {
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }
                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber2030O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Left:
                        if ((loc[2] - 3 < 0) || (loc[2] + 2 >= mapDepth) || (loc[1] - 7 < 0))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] - 1; x > loc[1] - 8; x--)
                            {
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                            }

                            if (orient1 == false)
                                break;
                        }

                        if (orient1 == false)
                            if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] - 5 < 0))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] - 1; x < loc[1] - 6; x--)
                                {
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }

                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber2030O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Right:
                        if ((loc[2] - 3 < 0) || (loc[2] + 2 >= mapDepth) || (loc[1] + 7 >= mapWidth))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] + 1; x < loc[1] + 8; x++)
                            {
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                            }

                            if (orient1 == false)
                                break;
                        }

                        if (orient1 == false)
                            if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] + 5 > mapWidth))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] + 1; x > loc[1] + 6; x++)
                                {
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }

                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber2030O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                }

                if ((!orient1) && (!orient2))
                    return false;
                break;
            case Room.Chamber3030:
                //This room is 6 tiles by 6 tiles

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] + 7 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 8; y++)
                            for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Up:
                        if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] - 7 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 8; y--)
                            for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Left:
                        if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] - 7 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 8; x--)
                            for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Right:
                        if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] + 7 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[1] + 8; x++)
                            for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                }
                break;

            case Room.Chamber3040O1:
                //This room is 6 tiles by 8 tiles
                //First, test entering on the small wall.
                //If that orientation is invalid, test entering on the long wall
                //If that orientation is valid, re-set the room to the proper ID
                //If both orientations are invalid, the room is invalid

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] + 9 >= mapDepth))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] + 1; y < loc[2] + 10; y++)
                            {
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] + 7 >= mapDepth))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] + 1; y < loc[2] + 8; y++)
                                {
                                    for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }
                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber3040O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Up:
                        if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] - 9 < 0))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] - 1; y > loc[2] - 10; y--)
                            {
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] - 7 < 0))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] - 1; y > loc[2] - 8; y--)
                                {
                                    for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }
                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber3040O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Left:
                        if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] - 9 < 0))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] - 1; x > loc[1] - 10; x--)
                            {
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                            }

                            if (orient1 == false)
                                break;
                        }

                        if (orient1 == false)
                            if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] - 7 < 0))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] - 1; x < loc[1] - 8; x--)
                                {
                                    for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }

                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber3040O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Right:
                        if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] + 9 >= mapWidth))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] + 1; x < loc[1] + 10; x++)
                            {
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }

                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] + 7 > mapWidth))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] + 1; x > loc[1] + 8; x++)
                                {
                                    for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }

                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber3040O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                }

                if ((!orient1) && (!orient2))
                    return false;
                break;

            case Room.Chamber4040:
                //This room is 8 tiles by 8 tiles

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] + 9 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 10; y++)
                            for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Up:
                        if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] - 9 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 10; y--)
                            for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Left:
                        if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] - 9 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 10; x--)
                            for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Right:
                        if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] + 9 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[1] + 10; x++)
                            for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                }
                break;

            case Room.Chamber4050O1:
                //This room is 8 tiles by 10 tiles
                //First, test entering on the small wall.
                //If that orientation is invalid, test entering on the long wall
                //If that orientation is valid, re-set the room to the proper ID
                //If both orientations are invalid, the room is invalid

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 5 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] + 11 >= mapDepth))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] + 1; y < loc[2] + 12; y++)
                            {
                                for (int x = loc[1] - 5; x < loc[1] + 6; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[1] - 5 < 0) || (loc[1] + 6 >= mapWidth) || (loc[2] + 9 >= mapDepth))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] + 1; y < loc[2] + 10; y++)
                                {
                                    for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }
                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber4050O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Up:
                        if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] - 11 < 0))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] - 1; y > loc[2] - 12; y--)
                            {
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[1] - 5 < 0) || (loc[1] + 6 >= mapWidth) || (loc[2] - 9 < 0))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] - 1; y > loc[2] - 10; y--)
                                {
                                    for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }
                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber4050O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Left:
                        if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] - 11 < 0))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] - 1; x > loc[1] - 12; x--)
                            {
                                for (int y = loc[2] - 5; y < loc[2] + 6; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }

                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[2] - 6 < 0) || (loc[2] + 5 >= mapDepth) || (loc[1] - 9 < 0))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] - 1; x < loc[1] - 10; x--)
                                {
                                    for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }

                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber4050O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Right:
                        if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] + 11 >= mapWidth))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] + 1; x < loc[1] + 12; x++)
                            {
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                            }

                            if (orient1 == false)
                                break;
                        }

                        if (orient1 == false)
                            if ((loc[2] - 6 < 0) || (loc[2] + 5 >= mapDepth) || (loc[1] + 9 > mapWidth))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] + 1; x > loc[1] + 10; x++)
                                {
                                    for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }

                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber4050O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                }

                if ((!orient1) && (!orient2))
                    return false;
                break;

            case Room.Chamber5080O1:
                //This room is 10 tiles by 16 tiles
                //First, test entering on the small wall.
                //If that orientation is invalid, test entering on the long wall
                //If that orientation is valid, re-set the room to the proper ID
                //If both orientations are invalid, the room is invalid

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 5 < 0) || (loc[1] + 6 >= mapWidth) || (loc[2] + 17 >= mapDepth))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] + 1; y < loc[2] + 18; y++)
                            {
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[1] - 8 < 0) || (loc[1] + 9 >= mapWidth) || (loc[2] + 11 >= mapDepth))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] + 1; y < loc[2] + 12; y++)
                                {
                                    for (int x = loc[1] - 8; x < loc[1] + 10; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }
                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber3040O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Up:
                        if ((loc[1] - 5 < 0) || (loc[1] + 6 >= mapWidth) || (loc[2] - 17 < 0))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] - 1; y > loc[2] - 18; y--)
                            {
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[1] - 8 < 0) || (loc[1] + 9 >= mapWidth) || (loc[2] - 11 < 0))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] - 1; y > loc[2] - 12; y--)
                                {
                                    for (int x = loc[1] - 8; x < loc[1] + 10; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }
                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber3040O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                    case Direction.Left:
                        if ((loc[2] - 6 < 0) || (loc[2] + 5 >= mapDepth) || (loc[1] - 17 < 0))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] - 1; x > loc[1] - 18; x--)
                            {
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }

                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (orient1 == false)
                            if ((loc[2] - 9 < 0) || (loc[2] + 8 >= mapDepth) || (loc[1] - 11 < 0))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] - 1; x > loc[1] - 11; x--)
                                {
                                    for (int y = loc[2] - 9; y < loc[2] + 9; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }

                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber3040O2;
                                else
                                    return false;
                            }
                        break;
                    case Direction.Right:
                        if ((loc[2] - 6 < 0) || (loc[2] + 5 >= mapDepth) || (loc[1] + 17 >= mapWidth))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] + 1; x < loc[1] + 18; x++)
                            {
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                    {
                                        orient1 = false;
                                        break;
                                    }
                            }

                            if (orient1 == false)
                                break;
                        }

                        if (orient1 == false)
                            if ((loc[2] - 9 < 0) || (loc[2] + 8 >= mapDepth) || (loc[1] + 11 > mapWidth))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] + 1; x > loc[1] + 12; x++)
                                {
                                    for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient2 = false;
                                            break;
                                        }

                                    if (orient2 == false)
                                        break;
                                }

                                if (orient2)
                                    id = Room.Chamber3040O2;
                            }

                        if ((!orient1) && (!orient2))
                            return false;

                        break;
                }

                if ((!orient1) && (!orient2))
                    return false;
                break;

            case Room.ChamberC30:
                //This room is a circle, 6 tiles in diameter

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] + 7 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 8; y++)
                            if ((y == loc[2] + 1) || (y > loc[2] + 5))
                            {
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Up:
                        if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] - 8 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 8; y--)
                            if ((y == loc[2] - 1) || (y < loc[2] - 5))
                            {
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Left:
                        if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] - 8 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 8; x--)
                            if ((x == loc[1] - 1) || (x < loc[1] - 5))
                            {
                                for (int y = loc[2] - 2; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int y = loc[2] - 3; y < loc[2] + 5; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Right:
                        if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] + 8 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[1] + 8; x++)
                            if ((x == loc[1] + 1) || (x > loc[1] + 5))
                            {
                                for (int y = loc[2] - 2; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int y = loc[2] - 2; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                }
                break;

            case Room.ChamberC50:
                //This room is a circle, 10 tiles in diameter

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 5 < 0) || (loc[1] + 6 >= mapWidth) || (loc[2] + 11 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 12; y++)
                            if ((y == loc[2] + 1) || (y > loc[2] + 9))
                            {
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((y < loc[2] + 4) || (y > loc[2] + 7))
                            {
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Up:
                        if ((loc[1] - 5 < 0) || (loc[1] + 6 >= mapWidth) || (loc[2] - 11 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 12; y--)
                            if ((y == loc[2] - 1) || (y < loc[2] - 9))
                            {
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((y > loc[2] - 4) || (y < loc[2] - 7))
                            {
                                for (int x = loc[2] - 4; x < loc[1] + 7; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Left:
                        if ((loc[2] - 6 < 0) || (loc[2] + 5 >= mapDepth) || (loc[1] - 11 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 12; x--)
                            if ((x == loc[1] - 1) || (x < loc[1] - 9))
                            {
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((x > loc[1] - 4) || (x < loc[1] - 7))
                            {
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int y =loc[2] - 6; y < loc[2] + 6; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Right:
                        if ((loc[2] - 6 < 0) || (loc[2] + 5 >= mapDepth) || (loc[1] + 11 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[1] + 12; x++)
                            if ((x == loc[1] + 1) || (x > loc[1] + 9))
                            {
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((x < loc[1] + 4) || (x > loc[1] + 7))
                            {
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                }
                break;

            case Room.ChamberO40:
                //This room is an octagon, 8 tiles in diameter

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] + 9 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 10; y++)
                            if ((y == loc[2] + 1) || (y >= loc[2] + 8))
                            {
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((y == loc[2] + 2) || (y == loc[2] + 7))
                            {
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Up:
                        if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] - 9 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 10; y--)
                            if ((y == loc[2] - 1) || (y <= loc[2] - 8))
                            {
                                for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((y == loc[2] - 2) || (y == loc[2] - 7))
                            {
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Left:
                        if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] - 9 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 10; x--)
                            if ((x == loc[1] - 1) || (x <= loc[1] - 8))
                            {
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((x == loc[1] - 2) || (x == loc[1] - 7))
                            {
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Right:
                        if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] + 9 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[1] + 10; x++)
                            if ((x == loc[1] + 1) || (x >= loc[1] + 8))
                            {
                                for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((x == loc[1] + 2) || (x == loc[1] + 7))
                            {
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                }
                break;

            case Room.ChamberO60:
                //This room is an octagon, 12 tiles in diameter

                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 6 < 0) || (loc[1] + 7 >= mapWidth) || (loc[2] + 13 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 14; y++)
                        {
                            if ((y == loc[2] + 1) || (y > loc[2] + 11))
                            {
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((y == loc[2] + 2) || (y == loc[2] + 11))
                            {
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((y == loc[2] + 3) || (y == loc[2] + 10))
                            {
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int x = loc[1] - 6; x < loc[1] + 8; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                    case Direction.Up:
                        if ((loc[1] - 6 < 0) || (loc[1] + 7 >= mapWidth) || (loc[2] - 13 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 14; y--)
                        {
                            if ((y == loc[2] - 1) || (y < loc[2] - 11))
                            {
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((y == loc[2] - 2) || (y == loc[2] - 11))
                            {
                                for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((y == loc[2] - 3) || (y == loc[2] - 10))
                            {
                                for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int x = loc[1] - 6; x < loc[1] + 8; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                    case Direction.Left:
                        if ((loc[2] - 7 < 0) || (loc[2] + 6 >= mapDepth) || (loc[1] - 13 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 14; x--)
                        {
                            if ((x == loc[1] - 1) || (x < loc[1] - 11))
                            {
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((x == loc[1] - 2) || (x == loc[1] - 11))
                            {
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((x == loc[1] - 3) || (x == loc[1] - 10))
                            {
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int y = loc[2] - 7; y < loc[2] + 7; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break; 
                    case Direction.Right:
                        if ((loc[2] - 7 < 0) || (loc[2] + 6 >= mapDepth) || (loc[1] + 13 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[1] + 14; x++)
                        {
                            if ((x == loc[1] + 1) || (x > loc[1] + 11))
                            {
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((x == loc[1] + 2) || (x == loc[1] + 11))
                            {
                                for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else if ((x == loc[1] + 3) || (x == loc[1] + 10))
                            {
                                for (int y = loc[2] - 6; y < loc[2] + 6; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                            }
                            else
                                for (int y = loc[2] - 7; y < loc[2] + 7; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        }
                        break;
                }
                break;

            case Room.ChamberTrap4060O1:
                //This chamber is a trapezoid
                //First, try a chamber 8 tiles wide that tapers to 4 tiles over 12 tiles
                //If that is invalid, try a chamber 4 tiles wide that tapers to 8 tiles over 12 tiles
                //If that is invalid, try a chamber 12 tiles wide that tapers to 8 tiles over 8 tiles
                //If that is invalid, try a chamber 8 tiles wide that tapers to 12 tiles over 8 tiles
                //Re-set the room to the first valid room.
                //If all are invalid, the room is invalid.

                switch (dir)
                {
                    #region Down
                    case Direction.Down:
                        //Test Orientation 1
                        if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] + 13 >= mapDepth))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] + 1; y < loc[2] + 14; y++)
                            {
                                //Test the room. If a location is occupied, Orientation 1 becomes invalid
                                if (y < loc[2] + 5)
                                {
                                    for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }
                                }
                                else if (y < loc[2] + 11)
                                {
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }
                                }
                                else
                                    for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }

                                //If at any time a location becomes invlaid, end testing on Orientation 1
                                if (orient1 == false)
                                    break;
                            }
                        }

                        //If Orientation 1 doesn't fit, try Orientation 2, if that works, set new room orientation
                        if (!orient1)
                            if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] + 13 >= mapDepth))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] + 1; y < loc[2] + 14; y++)
                                {
                                    if (y < loc[2] + 5)
                                    {
                                        for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    }
                                    else if (y < loc[2] + 11)
                                    {
                                        for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    if (orient2 == false)
                                        break;
                                }
                                if (orient2)
                                    id = Room.ChamberTrap4060O2;
                            }

                        //If Orientation 2 doesn't fit, try Orientation 3, if that works, set new room orientation
                        if ((!orient1) && (!orient2))
                            if ((loc[1] - 6 < 0) || (loc[1] + 7 >= mapWidth) || (loc[2] + 9 >= mapDepth))
                                orient3 = false;
                            else
                            {
                                for (int y = loc[2] + 1; y < loc[2] + 10; y++)
                                {
                                    if (y < loc[2] + 4)
                                    {
                                        for (int x = loc[1] - 6; x < loc[1] + 8; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else if (y < loc[2] + 8)
                                    {
                                        for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                }
                                if (orient3)
                                    id = Room.ChamberTrap4060O3;
                            }

                        //If Orientation 3 doesn't fit, try Orientation 4
                        if ((!orient1) && (!orient2) && (!orient3))
                            if ((loc[1] - 6 < 0) || (loc[1] + 7 >= mapWidth) || (loc[2] + 9 >= mapDepth))
                                orient4 = false;
                            else
                            {
                                for (int y = loc[2] + 1; y < loc[2] + 9; y++)
                                {
                                    if (y < loc[2] + 4)
                                    {
                                        for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient4 = false;
                                                break;
                                            }
                                    }
                                    else if (y < loc[2] + 8)
                                    {
                                        for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient4 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int x = loc[1] - 6; x < loc[1] + 8; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient4 = false;
                                                break;
                                            }
                                }
                                if (orient4)
                                    id = Room.ChamberTrap4060O4;
                            }
                        break;
                    #endregion
                    #region Up
                    case Direction.Up:
                        //Test Orientation 1
                        if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] - 13 < 0))
                            orient1 = false;
                        else
                        {
                            for (int y = loc[2] - 1; y > loc[2] - 14; y--)
                            {
                                //Test the room. If a location is occupied, Orientation 1 becomes invalid
                                if (y > loc[2] - 5)
                                {
                                    for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }
                                }
                                else if (y > loc[2] - 11)
                                {
                                    for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }
                                }
                                else
                                    for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }

                                //If at any time a location becomes invlaid, end testing on Orientation 1
                                if (orient1 == false)
                                    break;
                            }
                        }

                        //If Orientation 1 doesn't fit, try Orientation 2, if that works, set new room orientation
                        if (!orient1)
                            if ((loc[1] - 4 < 0) || (loc[1] + 5 >= mapWidth) || (loc[2] - 13 < 0))
                                orient2 = false;
                            else
                            {
                                for (int y = loc[2] - 1; y > loc[2] - 14; y--)
                                {
                                    if (y > loc[2] - 5)
                                    {
                                        for (int x = loc[1] - 2; x < loc[1] + 4; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    }
                                    else if (y > loc[2] - 11)
                                    {
                                        for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    if (orient2 == false)
                                        break;
                                }
                                if (orient2)
                                    id = Room.ChamberTrap4060O2;
                            }

                        //If Orientation 2 doesn't fit, try Orientation 3, if that works, set new room orientation
                        if ((!orient1) && (!orient2))
                            if ((loc[1] - 6 < 0) || (loc[1] + 7 >= mapWidth) || (loc[2] - 9 < 0))
                                orient3 = false;
                            else
                            {
                                for (int y = loc[2] - 1; y > loc[2] - 10; y--)
                                {
                                    if (y > loc[2] - 4)
                                    {
                                        for (int x = loc[1] - 6; x < loc[1] + 8; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else if (y > loc[2] - 8)
                                    {
                                        for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                }
                                if (orient3)
                                    id = Room.ChamberTrap4060O3;
                            }

                        //If Orientation 3 doesn't fit, try Orientation 4
                        if ((!orient1) && (!orient2) && (!orient3))
                            if ((loc[1] - 6 < 0) || (loc[1] + 7 >= mapWidth) || (loc[2] - 9 < 0))
                                orient4 = false;
                            else
                            {
                                for (int y = loc[2] - 1; y > loc[2] - 9; y--)
                                {
                                    if (y > loc[2] - 4)
                                    {
                                        for (int x = loc[1] - 4; x < loc[1] + 6; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient4 = false;
                                                break;
                                            }
                                    }
                                    else if (y > loc[2] - 8)
                                    {
                                        for (int x = loc[1] - 5; x < loc[1] + 7; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient4 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int x = loc[1] - 6; x < loc[1] + 8; x++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient4 = false;
                                                break;
                                            }
                                }
                                if (orient4)
                                    id = Room.ChamberTrap4060O4;
                            }
                        break;
                    #endregion
                    #region Left
                    case Direction.Left:
                        if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] - 13 < 0))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] - 1; x > loc[1] - 14; x--)
                            {
                                if (x > loc[1] - 5)
                                {
                                    for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }
                                }
                                else if (x > loc[1] - 11)
                                {
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }
                                }
                                else
                                    for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }

                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (!orient1)
                            if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] - 13 < 0))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] - 1; x > loc[1] - 14; x--)
                                {
                                    if (x > loc[1] - 5)
                                    {
                                        for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    }
                                    else if (x > loc[1] - 11)
                                    {
                                        for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }

                                    if (orient2 == false)
                                        break;
                                }
                                if (orient2)
                                    id = Room.ChamberTrap4060O2;
                            }

                        if ((!orient1) && (!orient2))
                            if ((loc[2] - 7 < 0) || (loc[2] + 7 >= mapDepth) || (loc[1] - 9 < 0))
                                orient3 = false;
                            else
                            {
                                for (int x = loc[1] - 1; x > loc[1] - 10; x--)
                                {
                                    if (x > loc[1] - 4)
                                    {
                                        for (int y = loc[2] - 7; y < loc[2] + 8; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else if (x > loc[1] - 8)
                                    {
                                        for (int y = loc[2] - 6; y < loc[2] + 7; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int y = loc[2] - 5; y < loc[2] + 6; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }

                                    if (orient3 == false)
                                        break;
                                }
                                if (orient3)
                                    id = Room.ChamberTrap4060O3;
                            }

                        if ((!orient1) && (!orient2) && (!orient3))
                            if ((loc[2] - 7 < 0) || (loc[2] + 7 >= mapDepth) || (loc[1] - 9 < 0))
                                orient4 = false;
                            else
                            {
                                for (int x = loc[1] - 1; x > loc[1] - 10; x--)
                                {
                                    if (x > loc[1] - 4)
                                    {
                                        for (int y = loc[2] - 5; y < loc[2] + 6; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else if (x > loc[1] - 8)
                                    {
                                        for (int y = loc[2] - 6; y < loc[2] + 7; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int y = loc[2] - 7; y < loc[2] + 8; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }

                                    if (orient3 == false)
                                        break;
                                }
                                if (orient4)
                                    id = Room.ChamberTrap4060O4;
                            }
                        break;
                    #endregion
                    #region Right
                    case Direction.Right:
                        if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] + 13 >= mapWidth))
                            orient1 = false;
                        else
                        {
                            for (int x = loc[1] + 1; x < loc[1] + 14; x++)
                            {
                                if (x < loc[1] + 5)
                                {
                                    for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }
                                }
                                else if (x < loc[1] + 11)
                                {
                                    for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }
                                }
                                else
                                    for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                        if (dungeon[loc[0], x, y] != Map.Blank)
                                        {
                                            orient1 = false;
                                            break;
                                        }

                                if (orient1 == false)
                                    break;
                            }
                        }

                        if (!orient1)
                            if ((loc[2] - 5 < 0) || (loc[2] + 4 >= mapDepth) || (loc[1] + 13 >= mapWidth))
                                orient2 = false;
                            else
                            {
                                for (int x = loc[1] + 1; x < loc[1] + 14; x++)
                                {
                                    if (x < loc[1] + 5)
                                    {
                                        for (int y = loc[2] - 3; y < loc[2] + 3; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    }
                                    else if (x < loc[1] + 11)
                                    {
                                        for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int y = loc[2] - 5; y < loc[2] + 5; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient2 = false;
                                                break;
                                            }

                                    if (orient2 == false)
                                        break;
                                }
                                if (orient2)
                                    id = Room.ChamberTrap4060O2;
                            }

                        if ((!orient1) && (!orient2))
                            if ((loc[2] - 7 < 0) || (loc[2] + 7 >= mapDepth) || (loc[1] + 9 >= mapWidth))
                                orient3 = false;
                            else
                            {
                                for (int x = loc[1] + 1; x < loc[1] + 10; x++)
                                {
                                    if (x < loc[1] + 4)
                                    {
                                        for (int y = loc[2] - 7; y < loc[2] + 8; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else if (x < loc[1] + 8)
                                    {
                                        for (int y = loc[2] - 6; y < loc[2] + 7; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int y = loc[2] - 5; y < loc[2] + 6; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }

                                    if (orient3 == false)
                                        break;
                                }
                                if (orient3)
                                    id = Room.ChamberTrap4060O3;
                            }

                        if ((!orient1) && (!orient2) && (!orient3))
                            if ((loc[2] - 7 < 0) || (loc[2] + 7 >= mapDepth) || (loc[1] + 9 >= mapWidth))
                                orient4 = false;
                            else
                            {
                                for (int x = loc[1] + 1; x < loc[1] + 10; x++)
                                {
                                    if (x < loc[1] + 4)
                                    {
                                        for (int y = loc[2] - 5; y < loc[2] + 6; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else if (x < loc[1] + 8)
                                    {
                                        for (int y = loc[2] - 6; y < loc[2] + 7; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }
                                    }
                                    else
                                        for (int y = loc[2] - 7; y < loc[2] + 8; y++)
                                            if (dungeon[loc[0], x, y] != Map.Blank)
                                            {
                                                orient3 = false;
                                                break;
                                            }

                                    if (orient3 == false)
                                        break;
                                }
                                if (orient4)
                                    id = Room.ChamberTrap4060O4;
                            }
                        break;
                        #endregion
                }

                if ((!orient1) && (!orient2) && (!orient3) && (!orient4))
                    return false;

                break;
            #endregion
            case Room.Stair:
                //RollStairs();
                break;

            case Room.BeyondPass:
                //This is a 4-tile long, 2-tile wide passage beyond a door
                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 1 < 0) || (loc[1] + 2 >= mapWidth) || (loc[2] + 5 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 6; y++)
                            for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Up:
                        if ((loc[1] - 1 < 0) || (loc[1] + 2 >= mapWidth) || (loc[2] - 5 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 6; y--)
                            for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Left:
                        if ((loc[2] - 2 < 0) || (loc[2] + 1 >= mapDepth) || (loc[1] - 5 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 6; x--)
                            for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                    case Direction.Right:
                        if ((loc[2] - 2 < 0) || (loc[2] + 1 >= mapDepth) || (loc[1] + 5 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[1] + 6; x++)
                            for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                if (dungeon[loc[0], x, y] != Map.Blank)
                                    return false;
                        break;
                }
                break;

            case Room.BeyondT:
                //This is a 2-tile wide passage beyond a door that ends in a T intersection
                switch (dir)
                {
                    case Direction.Down:
                        if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] + 5 >= mapDepth))
                            return false;
                        for (int y = loc[2] + 1; y < loc[2] + 6; y++)
                            if (y < loc[2] + 3)
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                {
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                                }
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Up:
                        if ((loc[1] - 3 < 0) || (loc[1] + 4 >= mapWidth) || (loc[2] - 5 < 0))
                            return false;
                        for (int y = loc[2] - 1; y > loc[2] - 6; y--)
                            if (y > loc[2] - 3)
                                for (int x = loc[1] - 1; x < loc[1] + 3; x++)
                                {
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                                }
                            else
                                for (int x = loc[1] - 3; x < loc[1] + 5; x++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Left:
                        if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] - 5 < 0))
                            return false;
                        for (int x = loc[1] - 1; x > loc[1] - 6; x--)
                            if (x > loc[1] - 3)
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                {
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                                }
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                    case Direction.Right:
                        if ((loc[2] - 4 < 0) || (loc[2] + 3 >= mapDepth) || (loc[1] + 5 >= mapWidth))
                            return false;
                        for (int x = loc[1] + 1; x < loc[1] + 6; x++)
                            if (x < loc[1] + 3)
                                for (int y = loc[2] - 2; y < loc[2] + 2; y++)
                                {
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                                }
                            else
                                for (int y = loc[2] - 4; y < loc[2] + 4; y++)
                                    if (dungeon[loc[0], x, y] != Map.Blank)
                                        return false;
                        break;
                }
                break;
        }

        //If no invaliding criteria are met, then the room is valid
        return true;
    }

    void DeadEnd()
    {
        //Set a dead end. If it's not a door, set it as a wall
        //If it is a door, determine which kind of door.
        if (dungeon[loc[0], loc[1], loc[2]] != Map.Door)
            dungeon[loc[0], loc[1], loc[2]] = Map.Wall;
        else
            dungeon[loc[0], loc[1], loc[2]] = RollDoor();
    }

    Map RollDoor()
    {
        //From a weighted list, randomly determine a door
        switch(Random.Range(1, 21))
        {
            case 11:
            case 12:
                return Map.WoodDoorL;
            case 13:
                return Map.StoneDoor;
            case 14:
                return Map.StoneDoorL;
            case 15:
                return Map.IronDoor;
            case 16:
                return Map.IronDoorL;
            case 17:
                return Map.Portcullis;
            case 18:
                return Map.PortcullisL;
            case 19:
                return Map.SecretDoor;
            case 20:
                return Map.SecretDoorL;
            default:
                return Map.WoodDoor;
        }
    }

    void FindWalls()
    {
        //Go through each floor tile by tile
        //For each tile, if any of the surrounding tiles are NOT blank and NOT wall
        //That tile should be a wall. Set it as such!
        for (int f = 0; f < numFloors; f++)
            for (int x = 0; x < mapWidth; x++)
                if (x == 0)
                {
                    for (int y = 0; y < mapDepth; y++)
                        if (dungeon[f, x, y] == Map.Blank)
                        {
                            if (y == 0)
                            {
                                if (
                                    ((dungeon[f, x, y + 1] != Map.Blank) && (dungeon[f, x, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y] != Map.Blank) && (dungeon[f, x + 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y + 1] != Map.Blank) && (dungeon[f, x + 1, y + 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                            else if (y == mapDepth - 1)
                            {
                                if (
                                    ((dungeon[f, x, y - 1] != Map.Blank) && (dungeon[f, x, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y] != Map.Blank) && (dungeon[f, x + 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y - 1] != Map.Blank) && (dungeon[f, x + 1, y - 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                            else
                            {
                                if (
                                    ((dungeon[f, x, y + 1] != Map.Blank) && (dungeon[f, x, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y] != Map.Blank) && (dungeon[f, x + 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y - 1] != Map.Blank) && (dungeon[f, x + 1, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x, y - 1] != Map.Blank) && (dungeon[f, x, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y + 1] != Map.Blank) && (dungeon[f, x + 1, y + 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                        }
                }
                else if (x == mapWidth - 1)
                {
                    for (int y = 0; y < mapDepth; y++)
                        if (dungeon[f, x, y] == Map.Blank)
                        {
                            if (y == 0)
                            {
                                if (
                                    ((dungeon[f, x - 1, y] != Map.Blank) && (dungeon[f, x - 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x, y + 1] != Map.Blank) && (dungeon[f, x, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x - 1, y + 1] != Map.Blank) && (dungeon[f, x - 1, y + 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                            else if (y == mapDepth - 1)
                            {
                                if (
                                    ((dungeon[f, x - 1, y] != Map.Blank) && (dungeon[f, x - 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x, y - 1] != Map.Blank) && (dungeon[f, x, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x - 1, y - 1] != Map.Blank) && (dungeon[f, x - 1, y - 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                            else
                            {
                                if (
                                    ((dungeon[f, x - 1, y] != Map.Blank) && (dungeon[f, x - 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x, y - 1] != Map.Blank) && (dungeon[f, x, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x, y + 1] != Map.Blank) && (dungeon[f, x, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x - 1, y + 1] != Map.Blank) && (dungeon[f, x - 1, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x - 1, y - 1] != Map.Blank) && (dungeon[f, x - 1, y - 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                        }
                }
                else
                {
                    for (int y = 0; y < mapDepth; y++)
                        if (dungeon[f,x,y] == Map.Blank)
                        {
                            if (y == 0)
                            {
                                if (
                                    ((dungeon[f, x - 1, y] != Map.Blank) && (dungeon[f, x - 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y] != Map.Blank) && (dungeon[f, x + 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x - 1, y + 1] != Map.Blank) && (dungeon[f, x - 1, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x, y + 1] != Map.Blank) && (dungeon[f, x, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y + 1] != Map.Blank) && (dungeon[f, x + 1, y + 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                            else if (y == mapDepth - 1)
                            {
                                if (
                                    ((dungeon[f, x - 1, y] != Map.Blank) && (dungeon[f, x - 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y] != Map.Blank) && (dungeon[f, x + 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x - 1, y - 1] != Map.Blank) && (dungeon[f, x - 1, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x, y - 1] != Map.Blank) && (dungeon[f, x, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y - 1] != Map.Blank) && (dungeon[f, x + 1, y - 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                            else
                            {
                                if (
                                    ((dungeon[f, x - 1, y] != Map.Blank) && (dungeon[f, x - 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y] != Map.Blank) && (dungeon[f, x + 1, y] != Map.Wall)) ||
                                    ((dungeon[f, x - 1, y + 1] != Map.Blank) && (dungeon[f, x - 1, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x, y + 1] != Map.Blank) && (dungeon[f, x, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y + 1] != Map.Blank) && (dungeon[f, x + 1, y + 1] != Map.Wall)) ||
                                    ((dungeon[f, x - 1, y - 1] != Map.Blank) && (dungeon[f, x - 1, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x, y - 1] != Map.Blank) && (dungeon[f, x, y - 1] != Map.Wall)) ||
                                    ((dungeon[f, x + 1, y - 1] != Map.Blank) && (dungeon[f, x + 1, y - 1] != Map.Wall))
                                    )
                                    dungeon[f, x, y] = Map.Wall;
                            }
                        }
                }

    }

    void DisplayMap()
    {
        float xloc, yloc;
        GameObject tile = sprites[0];

        //Go through each tile.
        //Display from the list of sprites at the accurate position on screen.
        //Each sprite is a game object of a sprite with a script to destroy itself
        for (int x = 0; x < mapWidth; x++)
            for (int y = 0; y < mapDepth; y++)
            {
                switch (dungeon[loc[0], x, y])
                    {
                        case Map.ChmUp2Pass:
                            tile = sprites[31];
                            break;
                        case Map.ChmUpPass:
                            tile = sprites[32];
                            break;
                        case Map.Enter:
                            tile = sprites[2];
                            break;
                        case Map.FalseDoor:
                            tile = sprites[19];
                            break;
                        case Map.IronDoor:
                            tile = sprites[13];
                            break;
                        case Map.IronDoorL:
                            tile = sprites[14];
                            break;
                        case Map.Passage:
                            tile = sprites[4];
                            break;
                        case Map.PassageBalcony:
                            tile = sprites[6];
                            break;
                        case Map.PassageTall:
                            tile = sprites[5];
                            break;
                        case Map.Pillar:
                            tile = sprites[7];
                            break;
                        case Map.Portcullis:
                            tile = sprites[15];
                            break;
                        case Map.PortcullisL:
                            tile = sprites[16];
                            break;
                        case Map.Room:
                            tile = sprites[3];
                            break;
                        case Map.SecretDoor:
                            tile = sprites[17];
                            break;
                        case Map.SecretDoorL:
                            tile = sprites[18];
                            break;
                        case Map.ShaftDwnChm:
                            tile = sprites[30];
                            break;
                        case Map.ShaftUpDwn:
                            tile = sprites[31];
                            break;
                        case Map.StDwn2Chm:
                            tile = sprites[26];
                            break;
                        case Map.StDwn2Pass:
                            tile = sprites[27];
                            break;
                        case Map.StDwn3Chm:
                            tile = sprites[28];
                            break;
                        case Map.StDwn3Pass:
                            tile = sprites[29];
                            break;
                        case Map.StDwnChm:
                            tile = sprites[20];
                            break;
                        case Map.StDwnDead:
                            tile = sprites[24];
                            break;
                        case Map.StDwnPass:
                            tile = sprites[22];
                            break;
                        case Map.StoneDoor:
                            tile = sprites[11];
                            break;
                        case Map.StoneDoorL:
                            tile = sprites[12];
                            break;
                        case Map.StUpChm:
                            tile = sprites[21];
                            break;
                        case Map.StUpDead:
                            tile = sprites[25];
                            break;
                        case Map.StUpPass:
                            tile = sprites[23];
                            break;
                        case Map.Wall:
                            tile = sprites[1];
                            break;
                        case Map.Well:
                            tile = sprites[8];
                            break;
                        case Map.WoodDoor:
                            tile = sprites[9];
                            break;
                        case Map.WoodDoorL:
                            tile = sprites[10];
                            break;
                        default:
                            tile = sprites[0];
                            break;
                    }
                xloc = x - (6 / sizer);
                yloc = y - (4 / sizer);
                Instantiate(tile, new Vector3(xloc * sizer, ((-1 * yloc) * sizer) + 0.8f, 0), Quaternion.identity, parent.transform);
            }
    }
}
