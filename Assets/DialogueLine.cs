using UnityEngine;
using System;
[Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea] public string text;
    public Quest questToGive;
}
