﻿using System.Collections.Generic;
using UnityEngine;

namespace Waterfall
{
  /// <summary>
  ///   Material color modifier
  /// </summary>
  public class EffectLightColorModifier : EffectModifier
  {
    public string colorName = "_Main";

    public FloatCurve rCurve = new();
    public FloatCurve gCurve = new();
    public FloatCurve bCurve = new();
    public FloatCurve aCurve = new();

    private Light[] l;
    public override bool ValidForIntegrator => !string.IsNullOrEmpty(colorName);

    public EffectLightColorModifier() : base()
    {
      modifierTypeName = "Light Color";
    }

    public EffectLightColorModifier(ConfigNode node) : this()
    {
      Load(node);
    }

    public override void Load(ConfigNode node)
    {
      base.Load(node);

      node.TryGetValue("colorName", ref colorName);
      rCurve.Load(node.GetNode("rCurve"));
      gCurve.Load(node.GetNode("gCurve"));
      bCurve.Load(node.GetNode("bCurve"));
      aCurve.Load(node.GetNode("aCurve"));
    }

    public override ConfigNode Save()
    {
      var node = base.Save();

      node.name = WaterfallConstants.LightColorModifierNodeName;
      node.AddValue("colorName", colorName);
      node.AddNode(Utils.SerializeFloatCurve("rCurve", rCurve));
      node.AddNode(Utils.SerializeFloatCurve("gCurve", gCurve));
      node.AddNode(Utils.SerializeFloatCurve("bCurve", bCurve));
      node.AddNode(Utils.SerializeFloatCurve("aCurve", aCurve));
      return node;
    }

    public override void Init(WaterfallEffect parentEffect)
    {
      base.Init(parentEffect);
      l = new Light[xforms.Count];
      for (int i = 0; i < xforms.Count; i++)
      {
        l[i] = xforms[i].GetComponent<Light>();
      }
    }

    public List<Color> Get(List<float> strengthList)
    {
      var colorList = new List<Color>();
      if (strengthList.Count > 1)
      {
        for (int i = 0; i < l.Length; i++)
        {
          colorList.Add(new(rCurve.Evaluate(strengthList[i]) + randomValue,
                            gCurve.Evaluate(strengthList[i]) + randomValue,
                            bCurve.Evaluate(strengthList[i]) + randomValue,
                            aCurve.Evaluate(strengthList[i]) + randomValue));
        }
      }
      else
      {
        for (int i = 0; i < l.Length; i++)
        {
          colorList.Add(new(rCurve.Evaluate(strengthList[0]) + randomValue,
                            gCurve.Evaluate(strengthList[0]) + randomValue,
                            bCurve.Evaluate(strengthList[0]) + randomValue,
                            aCurve.Evaluate(strengthList[0]) + randomValue));
        }
      }

      return colorList;
    }

    public Light GetLight() => l[0];

    public void ApplyMaterialName(string newColorName)
    {
      colorName = newColorName;
      parentEffect.ModifierParameterChange(this);
    }

    public override bool IntegratorSuitable(EffectIntegrator integrator) => integrator is EffectLightColorIntegrator i && i.colorName == colorName && integrator.transformName == transformName;

    public override EffectIntegrator CreateIntegrator() => new EffectLightColorIntegrator(parentEffect, this);
  }
}