// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AnimateLoadingText : MonoBehaviour
{
    private UnityEngine.UI.Text _loadingText;
    private int _timeElapsed = 0;
    private float _deltaTimeSum = 0;

    private readonly string[] _loading = new string[]
    {
        "L",
        "O",
        "A",
        "D",
        "I",
        "N",
        "G"
    };

    // Use this for initialization
    public void Start()
    {
        _loadingText = GetComponent<UnityEngine.UI.Text>();
        _loadingText.text = _loading[0];
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        _deltaTimeSum += Time.deltaTime;
        if ((int) _deltaTimeSum == 1)
        {
            _timeElapsed++;
            _deltaTimeSum = 0;
            _loadingText.text = GetLoadingText(_timeElapsed);
        }
    }

    private string GetLoadingText(int timeElapsed)
    {
        StringBuilder loadingText = new StringBuilder();
        for (int i = 0; i <= timeElapsed % _loading.Length; i++)
        {
            loadingText.Append(_loading[i]);
        }

        return loadingText.ToString();
    }
}