using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ButtonTest : MonoBehaviour
{
    public TMP_Text text;
    void Start()
    {

    }

    void Update()
    {

    }

    public void ClickButton()
    {
        text.text = "You clikced me!";
    }


}

