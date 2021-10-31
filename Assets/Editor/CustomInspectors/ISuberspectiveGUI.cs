using UnityEngine;
using UnityEngine.Rendering;

interface ISuberspectiveGUI {
    Shader GetShader();
    PassType[] GetPassTypes();
}