using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

public class ConnectedQuizbowlScript : MonoBehaviour
{

    static int moduleIdCounter = 1;
    int moduleId;
    int currentClue = 0;
    int ans = 0;
    int toss = 0;
    string selectedTossup = "";
    string answer = "";
    bool _isSolved = false;

    string queryGetRandomURL1 = "https://qbreader.org/api/set-list";
    string queryGetRandomURL2 = "https://qbreader.org/api/num-packets?setName=";
    string queryGetRandomURL3 = "https://qbreader.org/api/packet?setName=";
    string yourAnswer = "";
    string currentClueDisplay;

    public KMBombModule bombModule;

    string[] clues;
    List<string> clues2 = new List<string>();
    List<string> acceptableAnswers = new List<string>();

    string[] answers;

    bool reroll = false;
    bool showNext;
    bool nextTossup = false;
    bool showBuzz;
    bool activated;
    bool connecting = false;
    bool focused = false;
    bool Submit = false;
    bool autosolving = false;
    bool online = true;

    string TheLetters = "<eQWERTYUIOPASDFGHJKLZXCVBNM1234567890-'. ";

    private KeyCode[] TheKeys =
    {
        KeyCode.Backspace, KeyCode.Return,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,
        KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M,
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        KeyCode.Minus, KeyCode.Quote, KeyCode.Period, KeyCode.Space
    };

    public TextMesh Hint;
    public TextMesh Answering;
    public Renderer BuzzerLights;
    public Material[] colors;
    public KMAudio Audio;
    public AudioClip[] Buzzing;
    public KMSelectable Reroll;
    public KMSelectable Next;
    public KMSelectable Buzz;
    public KMSelectable Activate;
    public KMSelectable ModuleSelectable;

    // Use this for initialization
    void Awake()
    {
        moduleId = moduleIdCounter++;
        BuzzerLights.material = colors[0];

        Reroll.OnInteract += delegate () { RerollTossup(); return false; };
        Next.OnInteract += delegate () { buttonPress(); return false; };
        Buzz.OnInteract += delegate () { Buzzer(); return false; };
        Activate.OnInteract += delegate () { EnterMode(); return false; };

        if (Application.isEditor)
            focused = true;
        ModuleSelectable.OnFocus += delegate () { focused = true; };
        ModuleSelectable.OnDefocus += delegate () { focused = false; };
    }

    void Start()
    {
        Submit = false;

        StartCoroutine(Pinger());
        Hint.text = "Connecting...";
    }

    void RerollTossup()
    {
        Submit = false;
        StopCoroutine(Pinger());
        queryGetRandomURL1 = "https://qbreader.org/api/set-list";
        queryGetRandomURL2 = "https://qbreader.org/api/num-packets?setName=";
        queryGetRandomURL3 = "https://qbreader.org/api/packet?setName=";
        StartCoroutine(Pinger());
        Hint.text = "Connecting...";
    }

    void EnterMode()
    {
        activated = false;
        selectedTossup = selectedTossup.Replace('.', '÷');
        string helperTossup = "";
        
        for (int i = 0; i < selectedTossup.Length - 1; i++)
        {
            
            if (selectedTossup[i] == '÷' && selectedTossup[i + 1] == ' ')
            {
                if (i + 2 < selectedTossup.Length)
                {
                    if (Char.IsLower(selectedTossup[i + 2]))
                    {
                        helperTossup += @"÷";
                    }
                    else
                    {
                        helperTossup += @".";
                    }

                }
            }
            if (selectedTossup[i] == '÷' && selectedTossup[i + 1] != ' ')
            {
                if (Char.IsUpper(selectedTossup[i + 1]))
                {
                    helperTossup += @".";
                }
                else
                {
                    helperTossup += @"÷";
                }
            }
            else
            {
                helperTossup += selectedTossup[i];
            }
        }
        helperTossup += @".";
        helperTossup = helperTossup.Replace("<b>", "");
        helperTossup = helperTossup.Replace("</b>", "");
        List<string> temp = new List<string>();
        clues2 = helperTossup.Split(new string[] { "÷\"" }, StringSplitOptions.None).ToList();
        int g = clues2.Count - 1;
        
        for (int i = g; i >= 0; i--)
        {
            if(i == clues2.Count - 1)
            {
                
                List<string> tmp = new List<string>();
                foreach (string s in clues2)
                {
                    tmp = s.Split('.').ToList();
                    for (int j = 0; j < tmp.Count; j++)
                    {
                        tmp[j] += '.';
                        temp.Add(tmp[j]);
                    }
                }
            }
        }
        clues2 = temp;
        int[] quma = new int[clues2.Count];
        for (int i = 0; i < clues2.Count; i++)
        {
            int count = 0;
            while (count < clues2[i].Length && clues2[i][count] == '\"') count++;
            if (count % 2 == 1) quma[i] = 1;
            if (quma[i] == 1) clues2[i] += "\"";
        }
        
        //clues = Regex.Split(helperTossup, @"(?<=[\.!\?])\s+");

        clues = new string[clues2.Count];
        clues = clues2.ToArray();
        //for (int i = 0; i < clues.Length-1; i++)
        //{
        //    Debug.LogFormat("{0}, {1}", clues[i][clues[i].Length - 1], clues[i + 1][0]);
        //    if (clues[i][clues[i].Length - 1] == '.' && clues[i + 1][1] == '\"')
        //    {
        //        helperTossup += @".";
        //        helperTossup += "\"";
        //    }
        //}
        currentClueDisplay = clues[0];
        StartCoroutine(TextingClue());
    }

    // Update is called once per frame
    void Update()
    {
        if (reroll == false)
        {
            Reroll.gameObject.SetActive(false);
        }
        if (reroll == true)
        {
            Reroll.gameObject.SetActive(true);
        }
        if (showNext == false)
        {
            Next.gameObject.SetActive(false);
        }
        else
        {
            Next.gameObject.SetActive(true);
        }
        if (showBuzz == false)
        {
            Buzz.gameObject.SetActive(false);
        }
        else
        {
            Buzz.gameObject.SetActive(true);
        }
        if (activated == false)
        {
            Activate.gameObject.SetActive(false);
        }
        else
        {
            Activate.gameObject.SetActive(true);
        }

        if (Submit == true)
        {
            Answering.text = yourAnswer.Replace("$", yourAnswer);
            for (int i = 0; i < TheKeys.Count(); i++)
            {
                if (Input.GetKeyDown(TheKeys[i]))
                {
                    if (TheLetters[i].ToString() == "<".ToString())
                    {
                        if (_isSolved == false)
                        {
                            handleBack();
                        }
                    }
                    else if (TheLetters[i].ToString() == "e".ToString())
                    {
                        if (_isSolved == false)
                        {
                            handleEnter();
                        }
                    }
                    else
                    {
                        if (_isSolved == false)
                        {
                            handleKey(TheLetters[i]);
                        }
                    }
                }
            }
        }
    }

    void handleBack()
    {
        if (focused || autosolving)
        {
            if (yourAnswer.Length != 0)
            {
                yourAnswer = yourAnswer.Substring(0, yourAnswer.Length - 1);
            }
        }
    }

    void handleEnter()
    {
        if (focused || autosolving)
        {
            Debug.LogFormat("[Quizbowl #{0}] Submitted: {1}", moduleId, yourAnswer);

            StartCoroutine(AnswerCheck());
        }
    }

    void handleKey(char c)
    {
        if (focused || autosolving)
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.TypewriterKey, transform);
            if (yourAnswer.Length != 100)
            {
                yourAnswer = yourAnswer + c;
            }
        }
    }

    void buttonPress()
    {
        if (nextTossup)
        {
            Submit = false;
            nextTossup = false;
            showNext = false;
            StartCoroutine(Pinger());
            Hint.text = "Connecting...";
        }
        else
        {
            currentClue++;
            currentClueDisplay = clues[currentClue];
            StartCoroutine(TextingClue());
        }
    }

    void Buzzer()
    {
        int bu = Rnd.Range(0, 2);
        switch (bu)
        {
            case 0:
                Audio.PlaySoundAtTransform("buzz1", transform);
                break;
            case 1:
                Audio.PlaySoundAtTransform("buzz2", transform);
                break;
        }
        yourAnswer = "";
        Submit = true;
        showNext = false;
        showBuzz = false;
        BuzzerLights.material = colors[1];
    }

    IEnumerator TextingClue()
    {
        showNext = false;
        showBuzz = true;
        bool lettered = false;
        Hint.text = "";
        int spaceCount = 0;
        int letterCount = 0;
        for (int b = 0; b < currentClueDisplay.Length; b++)
        {
            if (Submit == true)
            {
                break;
            }
            if (currentClueDisplay[b] == '÷' && b > 10)
            {
                Hint.text += '.';
            }
            else if (currentClueDisplay[b] == '÷' && b < 10)
            {
                Hint.text += ' ';
            }
            else
            {
                Hint.text += currentClueDisplay[b].ToString();
            }
            if (currentClueDisplay[b].ToString() == " ")
            {
                spaceCount += 1;
                lettered = false;
                letterCount = 0;
            }
            if (currentClueDisplay[b].ToString() == "-")
            {
                spaceCount += 1;
                lettered = false;
                letterCount = 0;
            }
            if (currentClueDisplay[b].ToString() != " " && currentClueDisplay[b].ToString() != "-")
            {
                letterCount += 1;
            }
            if (letterCount >= 10 && lettered == false)
            {
                spaceCount += 1;
                lettered = true;
            }
            if (currentClueDisplay[b].ToString() == "\n")
            {
                spaceCount = 0;
            }
            if (spaceCount >= 6 && lettered == false)
            {
                Hint.text = (Hint.text + "\n " + "");
                spaceCount = 0;
            }

            yield return new WaitForSecondsRealtime(0.03f);
        }
        if ((Hint.text.Length >= currentClueDisplay.Length && currentClue < clues.Length - 2) || nextTossup)
        {
            showNext = true;
        }
    }

    IEnumerator TextingAnswer()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        BuzzerLights.material = colors[0];
    }

    IEnumerator Pinger()
    {
        reroll = false;
        string set = "";
        int packId= 0;
        connecting = true;
        WWW www1 = new WWW(queryGetRandomURL1);
        while (!www1.isDone) { yield return null; if (www1.error != null) break; };
        if (www1.error == null)
        {
            try
            {
                List<string> sets = new List<string>();
                var result = JObject.Parse(www1.text);
                sets = result["setList"].ToObject<List<string>>();
                int setId = Rnd.Range(0, sets.Count);
                set = sets[setId];
            }
            catch (JsonReaderException)
            {
                Debug.LogFormat("Cannot Get Set");
                yield break;
            }
            
        }
        
        queryGetRandomURL2 += set;
        queryGetRandomURL3 += set + "&packetNumber=";
        WWW www2 = new WWW(queryGetRandomURL2);
        while (!www2.isDone) { yield return null; if (www2.error != null) break; };
        if (www2.error == null)
        {
            try
            {
                var result = JObject.Parse(www2.text);
                int packAmount = Int32.Parse(result["numPackets"].ToString());
                packId = Rnd.Range(1, packAmount+1);
            }
            catch (JsonReaderException)
            {
                Debug.LogFormat("Cannot Get Pack ID");
                yield break;
            }

        }
        
        queryGetRandomURL3 += packId;
        WWW www3 = new WWW(queryGetRandomURL3);
        while (!www3.isDone) { yield return null; if (www3.error != null) break; };
        if (www3.error == null)
        {
            try
            {
                string possibleTossup = "";
                var result = JObject.Parse(www3.text);

                if (result["tossups"].ToString() == "[]")
                {
                    reroll = true;
                    Hint.text = "Please Press Reroll.";
                }
                else
                {
                    int number = Int32.Parse(result["tossups"].Last["number"].ToString());
                    int tossupNumber = Rnd.Range(0, number);
                    possibleTossup = result["tossups"][tossupNumber]["question"].ToString();
                    if (!reroll)
                    {
                        Debug.LogFormat("{0} Packet {1} Tossup {2}: {3}", set, packId, tossupNumber, possibleTossup);
                        selectedTossup = possibleTossup;
                        answer = result["tossups"][tossupNumber]["answer"].ToString();
                        if (selectedTossup.Contains("?"))
                        {
                            selectedTossup = selectedTossup.Replace("?", ".");
                        }
                        if (selectedTossup.Contains("”"))
                        {
                            selectedTossup = selectedTossup.Replace("”", "\"");
                        }
                        if (selectedTossup.Contains("“"))
                        {
                            selectedTossup = selectedTossup.Replace("“", "\"");
                        }


                        string[] chari3 = { "Ã³", "Ã­", "Ã¤", "Ã¶" };
                        string[] chari4 = { "ó", "í", "ä", "ö" };

                        for (int i = 0; i < chari3.Length; i++)
                        {
                            if (selectedTossup.Contains(chari3[i]))
                            {
                                selectedTossup = selectedTossup.Replace(chari3[i], chari4[i]);
                            }
                            if (answer.Contains(chari3[i]))
                            {
                                answer = answer.Replace(chari3[i], chari4[i]);
                            }
                        }
                        Debug.LogFormat("[Connected Quizbowl #{0}] Tossup: {1}", moduleId, selectedTossup);
                        Debug.LogFormat("[Connected Quizbowl #{0}] Answerline: {1}", moduleId, answer);
                        connecting = false;
                        activated = true;
                        Hint.text = "Connected";

                    }
                }
                
            }
            catch (JsonReaderException)
            {
                Debug.LogFormat("Cannot Get Tossup");
                yield break;
            }

        }
        else
        {
            string chari1 = "âáàäāçéèêḥíīïñóöșúū";
            string chari2 = "aaaaaceeehiiinoosuu";
            string[] chari3 = { "Ã³", "Ã­", "Ã¤", "Ã¶" };
            string[] chari4 = { "ó", "í", "ä", "ö" };
            for (int j = 0; j < acceptableAnswers.Count(); j++)
            {
                for (int i = 0; i < chari1.Length; i++)
                {
                    if (acceptableAnswers[j].Contains(chari1[i]))
                    {
                        acceptableAnswers[j] = acceptableAnswers[j].Replace(chari1[i], chari2[i]);
                    }
                }
                for (int i = 0; i < chari3.Length; i++)
                {
                    if (acceptableAnswers[j].Contains(chari3[i]))
                    {
                        acceptableAnswers[j] = acceptableAnswers[j].Replace(chari3[i], chari4[i]);
                    }
                }

            }
            connecting = true;
            online = false;
            toss = Rnd.Range(0, 200) * 2;
            ans = toss + 1;
            selectedTossup = TossupList.phrases[toss];
            answer = TossupList.phrases[ans];
            yield return new WaitForSecondsRealtime(1.0f);
            Debug.LogFormat("[Connected Quizbowl #{0}] Tossup: {1}", moduleId, selectedTossup);
            Debug.LogFormat("[Connected Quizbowl #{0}] Answerline: {1}", moduleId, answer);
            connecting = false;
            activated = true;
            Hint.text = "Connected";
        }
    }

    IEnumerator AnswerCheck()
    {
        bool right = false;
        if (!online)
        {
            
            if (answer != null && selectedTossup != "null")
            {

                if (answer.Contains(" or ".ToLower()))
                {
                    answers = Regex.Split(answer, " or ");
                    for (int i = 0; i < answers.Length; i++)
                    {
                        acceptableAnswers.Add(answers[i]);
                    }
                }
                else
                {
                    acceptableAnswers.Add(answer);
                }

            }
            for (int i = 0; i < acceptableAnswers.Count; i++)
            {
                if (!right)
                {
                    if (yourAnswer.ToUpper() == acceptableAnswers[i].ToUpper())
                    {
                        right = true;
                    }
                    else
                    {
                        right = false;
                    }
                }
            }
        }
        else
        {
            string queryGetRandomURL = "https://qbreader.org/api/check-answer?answerline=" + answer + "&givenAnswer=" + yourAnswer;
            WWW www = new WWW(queryGetRandomURL);
            while (!www.isDone) { yield return null; if (www.error != null) break; };
            if (www.error == null)
            {
                try
                {
                    var result = JObject.Parse(www.text);
                    string status = result["directive"].ToString();
                    Debug.LogFormat("Your Answer: {1}, That answer is {0}", status, yourAnswer);
                    if (status == "accept") right = true;
                }
                catch (JsonReaderException)
                {
                    Debug.LogFormat("Cannot Get Pack ID");
                    yield break;
                }

            }
        }
        if (right == true)
        {
            BuzzerLights.material = colors[1];
            Hint.text = "";
            Answering.text = "";
            _isSolved = true;
            bombModule.HandlePass();
        }
        else
        {
            bombModule.HandleStrike();
            BuzzerLights.material = colors[2];
            yourAnswer = "";
            Answering.text = "";
            Submit = false;
            StartCoroutine(TextingClue());
            StartCoroutine(TextingAnswer());
            if (currentClue >= clues.Length - 2)
            {
                clues[0] += "Press Next to get Next Tossup.";
                currentClueDisplay = clues[0];
                currentClue = 0;
                nextTossup = true;
                showBuzz = false;
            }
        }
    }

    //twitch plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} activate [Presses the activate button] | !{0} next [Presses the next button] | !{0} submit <ans> [Submits the specified answer 'ans']";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*activate\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (!activated)
            {
                yield return "sendtochaterror This button cannot be pressed right now!";
                yield break;
            }
            Activate.OnInteract();
        }
        if (Regex.IsMatch(command, @"^\s*next\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (!showNext)
            {
                yield return "sendtochaterror This button cannot be pressed right now!";
                yield break;
            }
            Next.OnInteract();
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify an answer to submit!";
            else
            {
                for (int i = 7; i < command.Length; i++)
                {
                    if (!"QWERTYUIOPASDFGHJKLZXCVBNM1234567890-'. ".Contains(command.ToUpper()[i]))
                    {
                        yield return "sendtochaterror!f The specified character '" + command[i] + "' is invalid!";
                        yield break;
                    }
                }
                if (!showBuzz)
                {
                    yield return "sendtochaterror You cannot submit an answer right now!";
                    yield break;
                }
                Buzz.OnInteract();
                yield return new WaitForSeconds(.1f);
                for (int i = 7; i < command.Length; i++)
                {
                    handleKey(command.ToUpper()[i]);
                    yield return new WaitForSeconds(.1f);
                }
                bool correct = false;
                
                if (correct)
                    yield return "solve";
                else
                    yield return "strike";
                handleEnter();
            }
        }
    }
}
