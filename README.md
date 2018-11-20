# MapGenerator
A D&D-inspired map generator



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
        
        I have included in the repository an example good map -- no overlaps -- and an example
        bad map -- many overlaps. Each room and passage should be clearly defined and surrounded
        by walls, with clearly defined shapes.

        I am NOT looking for optimization at this point -- I just want my script to work. I will
        worry about optimizing at a later point in time.
