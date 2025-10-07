using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using KModkit;

public class LegoRemoval : MonoBehaviour
{
    public KMAudio Audio; public KMBombInfo bombInf; public KMBombModule Module; 

    public AudioClip[] brickAudio;
    public AudioClip solveAudio;

    public Material[] colorMats;
    public Material[] plateColors;
    public Material[] italyMats;

    public GameObject[] bricks;
    public GameObject[] grid;
    public GameObject baseBricks;

    public MeshRenderer[] baseplate;
    public MeshRenderer[] screenMesh;

    public KMSelectable theModule;
    public KMSelectable buttonSelect;

    public TextMesh[] screenText;

    public BoxCollider boxOfStupid;


    List<List<List<int>>> modElemBoxes = new List<List<List<int>>>();

    //List<int> plateColors = new List<int> { 15, 4, 7, 10,     7, 12, 13, 0 };
    //List<int> plateColors = new List<int> { 0, 3, 6, 10,  10, 10, 15, 15 };
    List<int> plateColInd = new List<int> { 0, 1, 2, 3 };

    List<int> rotations = new List<int> { 0, 90, 180, 270 };
    //List<int> rotations = new List<int> { 180, 270, 360, 450 };

    //List<List<int>> buffer = new List<List<int>>();
    
    Dictionary<int, List<List<int>>> structureBounds = new Dictionary<int, List<List<int>>>();
    List<List<GameObject>> structure = new List<List<GameObject>>();
    List<KMSelectable> children = new List<KMSelectable>();

    //base and direction
    List<int> baseOrder; List<int> direction = new List<int>(); GameObject incBrick;
    
    //button
    int count = 0; float t = 0; int s = 0;
    Color cyan = new Color(0f, 0.75f, 0.75f);
    Color clear = Color.clear;   Color red = Color.red;

    
    private bool buttonPress = false; 
    private bool badBrick = false;
    private bool yay = false;
    int inc = 0; int brValue = 0; int coValue = 0; bool pendingInc = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved = false;


    public KMModSettings modSettings;
    public class ModSettingsJSON
    {
        //public bool RiskyStructureGeneration = false;
        public bool ColorOrderExcluded = false;
    }

    void Awake()
    {
        moduleId = moduleIdCounter++;

        bool onFrontFace = true;
        GameObject test; test = Instantiate(bricks[2], baseBricks.transform.position, Quaternion.Euler(0,0,0), baseBricks.transform);
        if (test.GetComponentInChildren<BoxCollider>().bounds.Intersects(boxOfStupid.bounds)) { onFrontFace = false; } Destroy(test);
        //Debug.Log(onFrontFace);

        modElemBoxes.Add(CoordsGen(0, 0, 4, 4, 0));
        modElemBoxes.Add(CoordsGen(12, 0, 4, 4, 0));
        modElemBoxes.Add(CoordsGen(0, 12, 4, 4, 0));
        modElemBoxes.Add(CoordsGen(12, 12, 4, 4, 0));

        int brickNum = 0;                                       // BRICKS
        modSettings.RefreshSettings();
        //bool riskyGen = JsonConvert.DeserializeObject<ModSettingsJSON>(modSettings.Settings).RiskyStructureGeneration;
        //if (riskyGen == false)
        //{
            brickNum = UnityEngine.Random.Range(50, 75); // BRICK NUMBERS    49,71  62,72    42,70 
        //} else { brickNum = UnityEngine.Random.Range(42, 71); }

        int layerNum = UnityEngine.Random.Range(7, 11);         // LAYERS
        //Debug.Log(brickNum + "  " + layerNum);
        //Debug.Log("riskyGen: " + riskyGen);

        plateColInd.Shuffle();
        for (int i30 = 0; i30 < 4; i30++) { baseplate[i30].material = plateColors[plateColInd[0]]; }
        for (int i40 = 4; i40 < 10; i40++) { baseplate[i40].material = plateColors[plateColInd[1]]; }
        for (int i50 = 10; i50 < 16; i50++) { baseplate[i50].material = plateColors[plateColInd[2]]; }

        int checker = brickNum;
        int[] layerCounts = new int[layerNum];
        int baseBriLayNum = (int)Math.Floor((double)brickNum / layerNum);
        for (int i7 = 0; i7 < layerNum; i7++)       { layerCounts[i7] = baseBriLayNum; checker -= baseBriLayNum; }
        for (int i8 = 0; i8 < checker; i8++)        { layerCounts[UnityEngine.Random.Range(0, layerNum)] += 1; }
        for (int i9 = 0; i9 < baseBriLayNum; i9++)  { layerCounts[UnityEngine.Random.Range(0, layerNum)] += 1; layerCounts[UnityEngine.Random.Range(0, layerNum)] -= 1; }
        Array.Sort(layerCounts); Array.Reverse(layerCounts);


        int brickId = 0;
        for (int layer = 0; layer < layerNum; layer++)      // iterate through layers, generate structure
        {
            List<GameObject> layBricks = new List<GameObject>();
            int failsafe = 0;

            for (int i20 = 0; /*failsafe != 14 */i20 < layerCounts[layer]; i20++)     // iterates through IntPar
            {
                bool Affirmative = false;
                failsafe = 0;

                while (!Affirmative)
                {
                    // generate size & rotation of brick
                    int size = 0;
                    switch (layer) { // limits smaller bricks from spawning on lower layers
                        case 0: size = (int)Math.Round((double)UnityEngine.Random.Range(3, 12)); break;
                        case 1: size = (int)Math.Round((double)UnityEngine.Random.Range(2, 12)); break;
                        case 2: size = (int)Math.Round((double)UnityEngine.Random.Range(1, 12)); break;
                        default: size = (int)Math.Round((double)UnityEngine.Random.Range(0, 12)); break; }
                    int rotIndex = (int)Math.Round((double)UnityEngine.Random.Range(0, 4));
                    int rot = rotations[rotIndex];

                    // generate spot to place brick on
                    int width = (int)Char.GetNumericValue(bricks[size].name[0]) - 1;
                    int length = (int)Char.GetNumericValue(bricks[size].name[2]) - 1;
                    int row = 0; int col = 0; int lowI = 1; int upI = 15; // 3,13 = 10x10   2,14 = 12x12,  1,15 = 14x14,   0,16 = 16x16 
                    switch (rot) { //limit where bricks can be placed based on rotation
                        case 0: row = UnityEngine.Random.Range(lowI, upI - width); col = UnityEngine.Random.Range(lowI, upI - length); break;
                        case 90: row = UnityEngine.Random.Range(lowI, upI - length); col = UnityEngine.Random.Range(lowI + width, upI); break;
                        case 180: row = UnityEngine.Random.Range(lowI + width, upI); col = UnityEngine.Random.Range(lowI + length, upI); break;
                        case 270: row = UnityEngine.Random.Range(lowI + length, upI); col = UnityEngine.Random.Range(lowI, upI - width); break; }

                    var newCoords = CoordsGen(row, col, width, length, rot);

                    if (PlaceableHere(layBricks, newCoords, layer))
                    {
                        // 0, 0     90, 180     180, 0      270, 180

                        int alig = 0; if (!onFrontFace) { alig = 180; if (rot == 90 || rot == 270) { rot += 180; } }
                        Vector3 point; point = grid[row].transform.Find(col.ToString()).transform.position;
                        GameObject newBrick;
                        newBrick = Instantiate(bricks[size], point, Quaternion.Euler(0, rot, alig), baseBricks.transform);

                        var position = newBrick.transform.localPosition; position.y = 1.2f * layer;
                        newBrick.transform.localPosition = position;

                        var child = newBrick.transform.GetChild(0);
                        child.transform.parent = grid[row].transform.Find(col.ToString()).transform;
                        Destroy(newBrick);

                        layBricks.Add(child.gameObject);
                        /*if (size != 0) {*/ structureBounds.Add(brickId, newCoords); //}
                        //else { structureBounds.Add(brickId, CoordsGen(row - 1, col - 1, 2, 2, 90)); }


                        if (layer < 10)
                        {
                            if (brickId < 10)
                            {
                                child.name += "_0" + brickId.ToString() + "_0" + layer.ToString() + "_" + rotIndex.ToString();
                            }

                            else
                            {
                                child.name += "_" + brickId.ToString() + "_0" + layer.ToString() + "_" + rotIndex.ToString();
                            }
                        }
                        else
                        {
                            if (brickId < 10)
                            {
                                child.name += "_0" + brickId.ToString() + "_" + layer.ToString() + "_" + rotIndex.ToString();
                            }

                            else
                            {
                                child.name += "_" + brickId.ToString() + "_" + layer.ToString() + "_" + rotIndex.ToString();
                            }
                        }
                        child.GetComponent<KMSelectable>().Parent = theModule;

                        brickId++;

                        int colour = UnityEngine.Random.Range(0, 28);
                        if (size == 2 && layer > 0)
                        {
                            int colo = 0;
                            foreach (var m in structure[layer - 1])
                            {
                                if (!m.name.Contains("stud") && Colliding(newCoords, Co(m)))
                                {
                                    colo = ColorGet(m.GetComponent<KMSelectable>()); break;
                                }
                            }
                            while (colo == Int32.Parse(colorMats[colour].name))
                            {
                                colour = UnityEngine.Random.Range(0, 28);
                            }    
                        }
                        
                        child.GetComponent<MeshRenderer>().material = colorMats[colour];
                        if (colour > 19) { child.GetComponent<MeshRenderer>().material.renderQueue = 3001 + layer; }
                        child.GetComponent<MeshRenderer>().enabled = true;

                        failsafe = 0;
                        Affirmative = true;
                    } else { Affirmative = false; }
                    
                    // makes sure module doesn't crash
                    /*if (riskyGen == false) {*/ failsafe++; //} 
                    if (failsafe == 16) { //Debug.Log(moduleId + ": failed at " + layer);
                    Affirmative = true; }
                    
                }
            }
            structure.Add(layBricks);
            //buffer.Clear();

        } // iterate through layers, generate structure


        for (int l = 0; l < structure.Count; l++) { // find all selectables
            foreach (GameObject bri in structure[l]) {
                if (!bri.name.Contains("beam") && !bri.name.Contains("stud")) {
                    if (l != (structure.Count - 1)) {
                        if (!structure[l + 1].Any(b => Colliding(Co(bri), Co(b)))) {
                            children.Add(bri.GetComponent<KMSelectable>()); } }
                    else { children.Add(bri.GetComponent<KMSelectable>()); } }

                else if (bri.name.Contains("beam")) {
                    if (l < (structure.Count - 3)) {
                        if (!structure[l + 3].Any(b => Colliding(Co(bri), Co(b)))) {
                            children.Add(bri.GetComponent<KMSelectable>()); } }
                    else { children.Add(bri.GetComponent<KMSelectable>()); } }

                else if (bri.name.Contains("stud")) { children.Add(bri.GetComponent<KMSelectable>()); } };
         }

        foreach (KMSelectable brick in children){ DelegateBrick(brick); }

        List<KMSelectable> ch = new List<KMSelectable>(); ch.AddRange(children); ch.Add(buttonSelect);
        buttonSelect.Parent = theModule;
        buttonSelect.OnInteract += delegate () 
        {
            if (!buttonPress && !badBrick && !moduleSolved) { buttonPress = true; StartCoroutine(PressButton()); }
            else { buttonPress = false; }
            return false;
        };

        theModule.Children = ch.ToArray();
        theModule.UpdateChildrenProperly();

    }

    bool PlaceableHere(List<GameObject> CurLay, List<List<int>> cur, int lay)
    {

        if (CurLay.Any(bri => Colliding(cur, Co(bri)))) { return false; }
        if (modElemBoxes.Any(elem => Colliding(cur, elem))) { return false; }

        if (lay > 0) {
            if (structure[lay - 1].Any(bri => Colliding(cur, Co(bri)) && bri.name.Contains("beam"))) { return false; }

            if (lay > 1) {
                if (structure[lay - 2].Any(bri => Colliding(cur, Co(bri)) && bri.name.Contains("beam"))) { return false; }

                if (lay > 2) {
                    if (structure[lay - 3].Any(bri => Colliding(cur, Co(bri)) && bri.name.Contains("beam"))) { return false; } } }

            if (structure[lay - 1].Any(bri => Colliding(cur, Co(bri)) && !bri.name.Contains("stud"))) {
                return true; }
            else { return false; }
        }

        return true;
    }


    void Start()
    {
        bool colorEx = JsonConvert.DeserializeObject<ModSettingsJSON>(modSettings.Settings).ColorOrderExcluded;
        baseOrder = bombInf.GetSerialNumberNumbers().ToList();
        List<char> direc = bombInf.GetSerialNumberLetters().ToList();

        int b113 = ConvertObj(structure).Where(bri => bri.name.Contains("beam")).Count();
        int b11 = ConvertObj(structure).Where(bri => bri.name.Contains("stud")).Count();

        for (int ugh = 0; ugh < baseOrder.Count; ugh++) { // calculate Base Sequence
            if (!colorEx) { baseOrder[ugh] = (baseOrder[ugh] + b113) % 4; }
                    else { baseOrder[ugh] = ((baseOrder[ugh] + b113) % 3) + 1; }
        }
        //Debug.Log("colorEx: " + colorEx);

        for (int hhh = 0; hhh < direc.Count; hhh++) { // calculate Direction
            decimal oh = direc[hhh] - 55; //Debug.Log(direc[hhh] + ": " + oh + ", " + (oh + 55));
            direction.Add((decimal.ToInt32(oh) + b11) % 2); }
        int stupid = 0; string la = "";
        foreach (GameObject bri in structure.Last()) {  // calculate Increment Brick
            stupid += SizeGet(bri.GetComponent<KMSelectable>());
            la += "," + SizeGet(bri.GetComponent<KMSelectable>());
        } incBrick = bricks[stupid % 12];
        la = incBrick.name + ": Sizes" + la + " = " + stupid + "%12 = " + (stupid % 12);

        //direction = new List<int> { 0 };
        //baseOrder = new List<int> { 1 };

        string b = ""; string d = ""; foreach (int num in baseOrder) { b += num; } foreach (int num in direction) { d += num;  }
        foreach (List<GameObject> dumb in structure) { count += dumb.Count; }

        string l1 = "       (Beams = " + b113 + ")";
        string l2 = "            (Studs = " + b11 + ")";

        Debug.LogFormat("[LEGO Removal #{0}] BrickNum: {1}", moduleId, count);
        Debug.LogFormat("[LEGO Removal #{0}] Base Sequence: {1}{2}", moduleId, b, l1);
        Debug.LogFormat("[LEGO Removal #{0}] Direction: {1}{2}", moduleId, d, l2);
        Debug.LogFormat("[LEGO Removal #{0}] New Increment Brick: {1}", moduleId, la /*incBrick.name*/);

        Debug.LogFormat("[LEGO Removal #{0}] ", moduleId);
        DisplaySeq(0);

    }

    List<GameObject> ConvertObj(List<List<GameObject>> stru)
    {
        List<GameObject> con = new List<GameObject>();
        foreach (List<GameObject> la in stru) { foreach (GameObject bri in la) { con.Add(bri.gameObject); } }
        return con;
    }
 
    void DisplaySeq(int incer)
    {
        string a = " -------------- ";
        switch (baseOrder[(inc + incer) % baseOrder.Count])
        {
            case 0: a += "(0) By Color ID, "; break;
            case 1: a += "(1) By Size, "; break;
            case 2: a += "(2) By Support Studs, "; break;
            case 3: a += "(3) By Layer #, "; break;
        }
        switch (direction[(inc + incer) % direction.Count])
        {
            case 0: a += "(0) highest first"; break;
            case 1: a += "(1) lowest first"; break;
        } a += " -------------- ";
        Debug.LogFormat("[LEGO Removal #{0}] {1}", moduleId, a);
    }
    void DisplayBrick(KMSelectable b, int val)
    {
        string a = ""; string d = "";
        foreach (var c in b.name)
        {
            if (c != '_') { a += c; }
            else { break; }
        } if (b.name.Contains(incBrick.name)) { d += "  <= Increment Brick"; }
        Debug.LogFormat("[LEGO Removal #{0}] Brick Removed:    {1} ({2}){3}", moduleId, val, a, d);
    }

    List<List<int>> Co(GameObject ga)
    {
        return structureBounds[Convert.ToInt32(
            (Char.GetNumericValue(ga.name[ga.name.Length - 7]) * 10)
            + Char.GetNumericValue(ga.name[ga.name.Length - 6]))];
    }
    List<List<int>> CoordsGen(int row, int col, int width, int length, int rot)
    {
        List<List<int>> coords = new List<List<int>>();

        List<int> tl = new List<int>();
        List<int> br = new List<int>();

        switch (rot)
        {
            case 0: // tl
                {
                    //opRow += width; opCol += length;

                    tl.AddRange(new List<int> { row, col });
                    br.AddRange(new List<int> { row + width, col + length });
                    break;
                }
            case 90: // tr
                {
                    //opRow += length; opCol -= width;

                    tl.AddRange(new List<int> { row, col - width });
                    br.AddRange(new List<int> { row + length, col });
                    break;
                }
            case 180: // br
                {
                    //opRow -= width; opCol -= length;

                    tl.AddRange(new List<int> { row - width, col - length });
                    br.AddRange(new List<int> { row, col });
                    break;
                }
            case 270: // bl
                {
                    //opRow -= length; opCol += width; int i = col;

                    tl.AddRange(new List<int> { row - length, col });
                    br.AddRange(new List<int> { row, col + width });
                    break;
                }
        }

        //Debug.Log(tl[0] + "," + tl[1] + "  " + br[0] + "," + br[1] + "            " + (width + 1) + "," + (length + 1));

        int r = tl[0];
        for (int c = tl[1]; c != (br[1] + 1); c++)
        {
            //Debug.Log(orig.name + ": " + r + "," + c);
            coords.Add(new List<int> { r, c });

            if (c == br[1]) { c = tl[1] - 1; r++; }
            if (r > br[0]) { c = br[1]; }
        }

        return coords;
    }

    bool Colliding(List<List<int>> Brick1, List<List<int>> Brick2)
    {
        foreach (List<int> co1 in Brick1)
        {
            foreach (List<int> co2 in Brick2)
            {
                if (co1[0] == co2[0] && co1[1] == co2[1])
                {
                    return true;
                }
            }
        }

        return false;
    }


    int ColorGet(KMSelectable b)
    {
        return Convert.ToInt32(
              (Char.GetNumericValue(b.GetComponent<MeshRenderer>().material.name[0]) * 100) 
            + (Char.GetNumericValue(b.GetComponent<MeshRenderer>().material.name[1]) * 10) 
            +  Char.GetNumericValue(b.GetComponent<MeshRenderer>().material.name[2]));
    }
    int SizeGet(KMSelectable b)
    {
        return Convert.ToInt32(
              Char.GetNumericValue(b.gameObject.name[0]) 
            * Char.GetNumericValue(b.gameObject.name[2]) 
            * Char.GetNumericValue(b.gameObject.name[4]));
    }
    int SupportsGet(KMSelectable b)
    {
        int lay = LayerGet(b);
        if (b.name.Contains("1x1")) { return 1; }
        if (lay == 0) { return SizeGet(b); }

        int supCount = 0;
        
        foreach (GameObject bri in structure[lay - 1]) {
                foreach (var co1 in Co(b.gameObject)) {
                    foreach (var co2 in Co(bri)) {
                        if (co1[0] == co2[0] && co1[1] == co2[1]) {
                            supCount++; } } } }
        if (lay > 2) {
            foreach (GameObject bri in structure[lay - 3]) {
                if (bri.name.Contains("beam")) {
                    foreach (var co1 in Co(b.gameObject)) {
                        foreach (var co2 in Co(bri)) {
                            if (co1[0] == co2[0] && co1[1] == co2[1]) {
                                supCount++; } } } } } }

        return supCount;
    }
    int LayerGet(KMSelectable b)
    {
        return Convert.ToInt32(
            (Char.GetNumericValue(b.name[b.name.Length - 4]) * 10)
            + Char.GetNumericValue(b.name[b.name.Length - 3]));
    }

    int OrderGet(KMSelectable b, int orderInd)
    {
        int val = 0;
        switch (orderInd)
        {
            case 0: { val = ColorGet(b); break; }
            case 1: { val = SizeGet(b); break; }
            case 2: { val = SupportsGet(b); break; }
            case 3: { val = LayerGet(b); break; }
        }
        return val;
    }

    void PressBrick(KMSelectable theBrick)
    {
        if (!moduleSolved && !badBrick)
        {
            bool valid = false;
            if (pendingInc) { /*Debug.Log(pendingInc);*/ inc++; pendingInc = false; }

            int baseIdx = baseOrder[inc % baseOrder.Count];
            int dirIdx = direction[inc % direction.Count];
            DisplayBrick(theBrick, OrderGet(theBrick, baseIdx));

            var order = children
                .GroupBy(bri => OrderGet(bri, baseIdx)).ToArray()
                .OrderBy(grp => grp.Key).ToArray();

            switch (dirIdx)
            {
                case 1:
                    {
                        if (order.ElementAtOrDefault(0).Key == OrderGet(theBrick, baseIdx)) { valid = true; }
                        else
                        {
                            valid = false;
                            coValue = order.ElementAtOrDefault(0).Key;
                        }
                        break;
                    }
                case 0:
                    {
                        if (order.ElementAtOrDefault(order.Count() - 1).Key == OrderGet(theBrick, baseIdx)) { valid = true; }
                        else
                        {
                            valid = false;
                            coValue = order.ElementAtOrDefault(order.Count() - 1).Key;
                        }
                        break;
                    }
            }

            switch (valid)
            {
                case true:
                    {
                        count--; children.RemoveAll(x => x.name == theBrick.name);

                        if (theBrick.name.Contains(incBrick.name)) { pendingInc = true; DisplaySeq(1); }

                        int lay = LayerGet(theBrick);
                        var cur = Co(theBrick.gameObject);
                        List<GameObject> CurLayer = structure[lay]; CurLayer.RemoveAll(b => b.name == theBrick.name);
                        //Debug.Log("Selected Brick: " + theBrick.name);


                        if (lay > 0)
                        {
                            //find foundation
                            var foundation = structure[lay - 1].Where(bri =>
                               !bri.name.Contains("stud") &&
                               Colliding(cur, Co(bri)));

                            if (lay > 2)
                            {
                                var lay3B = structure[lay - 3].Where(bri =>
                                    bri.name.Contains("beam") &&
                                    Colliding(cur, Co(bri))) ?? Enumerable.Empty<GameObject>();
                                foundation = foundation.Concat(lay3B);
                            }

                            //filter foundation
                            List<GameObject> checker = new List<GameObject>();
                            if (CurLayer.Count > 0)
                            {
                                foreach (GameObject bri in foundation)
                                {
                                    if (!CurLayer.Any(above => Colliding(Co(bri), Co(above))))
                                    {
                                        checker.Add(bri);
                                    }
                                }
                            }
                            else { checker.AddRange(foundation); }

                            // delegate
                            if (checker.Count > 0)
                            {
                                foreach (GameObject brick in checker)
                                {
                                    DelegateBrick(brick.GetComponent<KMSelectable>()); children.Add(brick.GetComponent<KMSelectable>());
                                }
                            }
                            checker.Clear();
                        }

                        //Debug.LogFormat("[LEGO Removal #{0}] DESTROYING {1}", moduleId, theBrick.name);
                        //Debug.Log(children.Count);

                        if (children.Count == 0)
                        {
                            moduleSolved = true; buttonPress = false;
                            GetComponent<KMBombModule>().HandlePass();
                            foreach (MeshRenderer sc in screenMesh) { sc.material = italyMats[2]; }

                            screenText[0].text = "Y";
                            screenText[1].text = "A";
                            screenText[2].text = "Y !";

                            children.Add(theBrick); yay = true; StartCoroutine(PassIndicator());
                            Audio.PlaySoundAtTransform(solveAudio.name, theBrick.gameObject.transform);
                            Debug.LogFormat("[LEGO Removal #{0}] YAY WEE YIPEE YAY WEE YAY WEE YA YIPEE YAY", moduleId);

                            theModule.Children = new KMSelectable[0];
                            theModule.UpdateChildrenProperly();

                            return;
                        }

                        if (SupportsGet(theBrick) > 2)
                        { Audio.PlaySoundAtTransform(brickAudio[0].name, theBrick.gameObject.transform); }
                        else { Audio.PlaySoundAtTransform(brickAudio[1].name, theBrick.gameObject.transform); }

                        Destroy(theBrick.gameObject); children.RemoveAll(item => item == null);
                        List<KMSelectable> ch = new List<KMSelectable>(); ch.AddRange(children); ch.Add(buttonSelect);

                        theModule.Children = ch.ToArray();
                        theModule.UpdateChildrenProperly();

                        break;
                    }
                case false:
                    {
                        buttonPress = false;

                        string la = ""; int stupid = 0; var top = new List<GameObject>();
                        for (int l = structure.Count - 1; l >= 0; l--) { if (structure[l].Count > 0) { top = structure[l]; break; } }
                        foreach (GameObject bri in top)
                        {
                            stupid += SizeGet(bri.GetComponent<KMSelectable>());
                            la += "," + SizeGet(bri.GetComponent<KMSelectable>());
                        }
                        la = bricks[stupid % 12].name + ": Sizes" + la + " = " + stupid + "%12 = " + (stupid % 12);
                        incBrick = bricks[stupid % 12]; inc = 0;

                        Debug.LogFormat("[LEGO Removal #{0}] !!!!!!!!!!!!!! HEY! Wrong brick! !!!!!!!!!!!!!!", moduleId);
                        Debug.LogFormat("[LEGO Removal #{0}] Correct Value: {1}", moduleId, coValue);
                        Debug.LogFormat("[LEGO Removal #{0}] New Increment Brick: {1}", moduleId, la /*incBrick.name*/);
                        DisplaySeq(0);
                        badBrick = true; StartCoroutine(StrikeIndicator());
                        GetComponent<KMBombModule>().HandleStrike(); break;
                    }
            }

            //}
            /*
            else {
                string guh = "";
                foreach (var co in Co(theBrick.gameObject)) {
                    guh += co[0] + "," + co[1] + "  "; }
                Debug.Log(theBrick.name + ":  " + guh);
            }
            */
        }
    }

    void DelegateBrick(KMSelectable brick)
    {
        
        brick.OnHighlight += delegate {
            if (!moduleSolved)
            {
                string txt = brick.GetComponent<MeshRenderer>().material.name;
                screenText[0].text = txt[0].ToString();
                screenText[1].text = txt[1].ToString();
                screenText[2].text = txt[2].ToString();
            }
        };
        brick.OnHighlightEnded += delegate {
            if (!moduleSolved)
            {
                screenText[0].text = "";
                screenText[1].text = "";
                screenText[2].text = "";
            }
        };

        brick.OnInteract = null;
        brick.OnInteract += delegate () { KMSelectable pressedBrick = brick; PressBrick(pressedBrick); return false; };
    }

    
    IEnumerator PassIndicator()
    {
        t = 0f;
        while (yay)
        {
            children[0].GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.Lerp(Color.clear, Color.green, t));
            if (t > .99f) { t = 0f; yay = false; }
            else { t += .025f; }

            yield return new WaitForSeconds(0.01f);
        }
    }
    IEnumerator StrikeIndicator()
    {
        screenText[0].text = "H";
        screenText[1].text = "E";
        screenText[2].text = "Y !";

        foreach (MeshRenderer sc in screenMesh) { sc.material = italyMats[1]; }
        s = 0; t = 0f;

        while (badBrick)
        {
            if (s == 2) {
                s = 0;
                foreach (TextMesh sc in screenText) { sc.text = ""; }
                foreach (MeshRenderer sc in screenMesh) { sc.material = italyMats[0]; }
                badBrick = false;
            }

            if (s == 0){
                foreach (var sel in ConvertObj(structure)){
                    sel.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.Lerp(Color.clear, red, t));}}

            else if (s == 1){
                foreach (var sel in ConvertObj(structure)){
                    sel.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.Lerp(red, Color.clear, t));}}

            if (t > .99f) { t = 0f; s++; }
            else /*if (s < 2)*/ { t += .033f; }
            /*else { t += .05f; }*/

            yield return new WaitForSeconds(0.01f);
        }
    }
    IEnumerator PressButton() {
        while (buttonPress)
        {
            if (s == 3) { s = 0; }

            if (s == 0){
                foreach (KMSelectable sel in children){
                    sel.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.Lerp(Color.clear, cyan, t));}}

            else if (s == 1){
                foreach (KMSelectable sel in children){
                    sel.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.Lerp(cyan, Color.clear, t));}}

            if (t > .99f) { t = 0f; s++; }
            else /*if (s < 2)*/ { t += .033f; }
            /*else { t += .05f; }*/

            yield return new WaitForSeconds(0.01f);
        }

        t = 0; s = 0;
        foreach (KMSelectable sel in children) {
            sel.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", clear); }
    }

}
