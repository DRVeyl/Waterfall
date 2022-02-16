﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Waterfall
{
  /// <summary>
  ///   This class represents a single effect. An effect contains a MODEL with various PROPERTIES, modified by EFFECTS
  /// </summary>
  public class WaterfallEffect
  {
    public          string                           name       = "";
    public          string                           parentName = "";
    public readonly List<Vector3>                    baseScales = new();
    public          ModuleWaterfallFX                parentModule;
    public          WaterfallEffectTemplate          parentTemplate;
    public readonly List<EffectFloatIntegrator>      floatIntegrators = new();
    public readonly List<EffectLightFloatIntegrator> lightFloatIntegrators = new();
    public readonly List<EffectLightColorIntegrator> lightColorIntegrators = new();
    public readonly List<EffectPositionIntegrator>   positionIntegrators = new();
    public readonly List<EffectRotationIntegrator>   rotationIntegrators = new();
    public readonly List<EffectScaleIntegrator>      scaleIntegrators = new();
    public readonly List<EffectColorIntegrator>      colorIntegrators = new();

    protected           WaterfallModel       model;
    protected readonly  List<EffectModifier> fxModifiers = new ();
    protected           Transform            parentTransform;
    protected           ConfigNode           savedNode;
    protected           bool                 effectVisible = true;
    protected           Vector3              savedScale;
    protected readonly  List<Transform>      effectTransforms = new();
    private   readonly  List<Material>       effectRendererMaterials = new();
    private   readonly  List<Transform>      effectRendererTransforms = new();
    private   readonly  List<Renderer>       effectRenderers = new();

    public WaterfallEffect() { }

    public WaterfallEffect(string parent, WaterfallModel mdl, WaterfallEffectTemplate templateOwner = null)
    {
      parentName             = parent;
      model                  = mdl;
      TemplatePositionOffset = templateOwner != null ? parentTemplate.position : Vector3.zero;
      TemplateRotationOffset = templateOwner != null ? parentTemplate.rotation : Vector3.zero;
      TemplateScaleOffset    = templateOwner != null ? parentTemplate.scale : Vector3.one;
    }

    public WaterfallEffect(ConfigNode node, WaterfallEffectTemplate templateOwner = null)
    {
      if (templateOwner != null)
        parentTemplate = templateOwner;
      TemplatePositionOffset = Vector3.zero;
      TemplateRotationOffset = Vector3.zero;
      TemplateScaleOffset    = Vector3.one;
      Load(node);
    }

    public WaterfallEffect(ConfigNode node, Vector3 positionOffset, Vector3 rotationOffset, Vector3 scaleOffset)
    {
      TemplatePositionOffset = positionOffset;
      TemplateRotationOffset = rotationOffset;
      TemplateScaleOffset    = scaleOffset;

      Load(node);
    }

    public WaterfallEffect(WaterfallEffect fx, Vector3 positionOffset, Vector3 rotationOffset, Vector3 scaleOffset, string overrideTransformName)
    {
      TemplatePositionOffset = positionOffset;
      TemplateRotationOffset = rotationOffset;
      TemplateScaleOffset    = scaleOffset;
      if (overrideTransformName != "")
        fx.savedNode.SetValue("parentName", overrideTransformName, true);
      Load(fx.savedNode);
    }

    public WaterfallEffect(WaterfallEffect fx, WaterfallEffectTemplate templateOwner)
    {
      parentTemplate         = templateOwner;
      TemplatePositionOffset = parentTemplate.position;
      TemplateRotationOffset = parentTemplate.rotation;
      TemplateScaleOffset    = parentTemplate.scale;

      if (parentTemplate.overrideParentTransform != "" && parentTemplate.overrideParentTransform != null)
        fx.savedNode.SetValue("parentName", parentTemplate.overrideParentTransform, true);

      Load(fx.savedNode);
    }

    public WaterfallEffect(WaterfallEffect fx)
    {
      parentTemplate         = fx.parentTemplate;
      TemplatePositionOffset = fx.TemplatePositionOffset;
      TemplateRotationOffset = fx.TemplateRotationOffset;
      TemplateScaleOffset    = fx.TemplateScaleOffset;
      Load(fx.Save());
    }

    public Vector3 TemplatePositionOffset { get; set; }
    public Vector3 TemplateRotationOffset { get; set; }
    public Vector3 TemplateScaleOffset    { get; set; }

    public List<EffectModifier> FXModifiers => fxModifiers;

    public WaterfallModel FXModel => model;

    public ConfigNode SavedNode => savedNode;

    /// <summary>
    ///   Load the node from config
    /// </summary>
    /// <param name="node"></param>
    public void Load(ConfigNode node)
    {
      savedNode = node;
      node.TryGetValue(nameof(name), ref name);

      if (!node.TryGetValue(nameof(parentName), ref parentName))
      {
        Utils.LogError(String.Format("[WaterfallEffect]: EFFECT with name {0} does not define parentName, which is required", name));
        return;
      }

      model       = new(node.GetNode(WaterfallConstants.ModelNodeName));
      fxModifiers.Clear();

      // types
      var positionNodes = node.GetNodes(WaterfallConstants.PositionModifierNodeName);
      var rotationNodes = node.GetNodes(WaterfallConstants.RotationModifierNodeName);
      var scalingNodes  = node.GetNodes(WaterfallConstants.ScaleModifierNodeName);
      var colorNodes    = node.GetNodes(WaterfallConstants.ColorModifierNodeName);
      var uvOffsetNodes = node.GetNodes(WaterfallConstants.UVScrollModifierNodeName);
      var floatNodes    = node.GetNodes(WaterfallConstants.FloatModifierNodeName);

      var colorLightNodes = node.GetNodes(WaterfallConstants.ColorFromLightNodeName);

      var lightFloatNodes = node.GetNodes(WaterfallConstants.LightFloatModifierNodeName);
      var lightColorNodes = node.GetNodes(WaterfallConstants.LightColorModifierNodeName);

      foreach (var subNode in positionNodes)
      {
        fxModifiers.Add(new EffectPositionModifier(subNode));
      }

      foreach (var subNode in rotationNodes)
      {
        fxModifiers.Add(new EffectRotationModifier(subNode));
      }

      foreach (var subNode in scalingNodes)
      {
        fxModifiers.Add(new EffectScaleModifier(subNode));
      }

      foreach (var subNode in colorNodes)
      {
        fxModifiers.Add(new EffectColorModifier(subNode));
      }

      foreach (var subNode in uvOffsetNodes)
      {
        fxModifiers.Add(new EffectUVScrollModifier(subNode));
      }

      foreach (var subNode in floatNodes)
      {
        fxModifiers.Add(new EffectFloatModifier(subNode));
      }

      foreach (var subNode in colorLightNodes)
      {
        fxModifiers.Add(new EffectColorFromLightModifier(subNode));
      }

      foreach (var subNode in lightFloatNodes)
      {
        fxModifiers.Add(new EffectLightFloatModifier(subNode));
      }

      foreach (var subNode in lightColorNodes)
      {
        fxModifiers.Add(new EffectLightColorModifier(subNode));
      }
    }

    public ConfigNode Save()
    {
      var node = new ConfigNode();
      node.name = WaterfallConstants.EffectNodeName;
      node.AddValue("name",       name);
      node.AddValue("parentName", parentName);
      node.AddNode(model.Save());
      foreach (var fx in fxModifiers)
      {
        node.AddNode(fx.Save());
      }

      return node;
    }

    public void CleanupEffect()
    {
      Utils.Log($"[WaterfallEffect]: Deleting effect {name}", LogType.Effects);
      for (int i = model.modelTransforms.Count - 1; i >= 0; i--)
      {
        Object.Destroy(model.modelTransforms[i].gameObject);
      }
    }

    public void InitializeEffect(ModuleWaterfallFX host, bool fromNothing, bool useRelativeScaling)
    {
      parentModule = host;
      var parents = parentModule.part.FindModelTransforms(parentName);
      Utils.Log($"[WaterfallEffect]: Initializing effect {name} at {parentName} [{parents.Length} instances]; relative scaling: {useRelativeScaling}", LogType.Effects);

      effectTransforms.Clear();
      baseScales.Clear();

      for (int i = 0; i < parents.Length; i++)
      {
        var effect          = new GameObject($"Waterfall_FX_{name}_{i}");
        var effectTransform = effect.transform;

        if (parents[i] == null)
        {
          Utils.LogError($"[WaterfallEffect]: Trying to attach effect to null parent transform {parentName} on model");
          continue;
        }

        effectTransform.SetParent(parents[i], true);
        effectTransform.localPosition    = Vector3.zero;
        effectTransform.localEulerAngles = Vector3.zero;
        if (useRelativeScaling)
          effectTransform.localScale = Vector3.one;

        model.Initialize(effectTransform, fromNothing);

        baseScales.Add(effectTransform.localScale);
        Utils.Log($"[WaterfallEffect] Scale: {effectTransform.localScale}", LogType.Effects);

        effectTransform.localPosition    = TemplatePositionOffset;
        effectTransform.localEulerAngles = TemplateRotationOffset;
        effectTransform.localScale       = Vector3.Scale(baseScales[i], TemplateScaleOffset);

        Utils.Log($"[WaterfallEffect] local Scale {effectTransform.localScale}, baseScale, {baseScales[i]}, {Vector3.Scale(baseScales[i], TemplateScaleOffset)}", LogType.Effects);

        Utils.Log($"[WaterfallEffect] Applied template offsets {TemplatePositionOffset}, {TemplateRotationOffset}, {TemplateScaleOffset}", LogType.Effects);

        effectTransforms.Add(effectTransform);
      }

      foreach (var fx in fxModifiers)
      {
        fx.Init(this);
      }

      effectRenderers.Clear();
      effectRendererMaterials.Clear();
      effectRendererTransforms.Clear();
      foreach (var t in model.modelTransforms)
      {
        foreach (var r in t.GetComponentsInChildren<Renderer>())
        {
          effectRenderers.Add(r);
          effectRendererMaterials.Add(r.material);
          effectRendererTransforms.Add(r.transform);
        }
      }

      InitializeIntegrators();
    }

    public void InitializeIntegrators()
    {
      floatIntegrators.Clear();
      positionIntegrators.Clear();
      colorIntegrators.Clear();
      rotationIntegrators.Clear();
      scaleIntegrators.Clear();
      lightFloatIntegrators.Clear();
      lightColorIntegrators.Clear();

      foreach (var mod in fxModifiers)
      {
        if (mod is EffectFloatModifier e) ParseFloatModifier(e);
        else if (mod is EffectColorModifier c) ParseColorModifier(c);
        else if (mod is EffectPositionModifier p) ParsePositionModifier(p);
        else if (mod is EffectRotationModifier r) ParseRotationModifier(r);
        else if (mod is EffectScaleModifier s) ParseScaleModifier(s);
        else if (mod is EffectLightFloatModifier light) ParseLightFloatModifier(light);
        else if (mod is EffectLightColorModifier lightColor) ParseLightColorModifier(lightColor);
      }
    }

    public void ApplyTemplateOffsets(Vector3 position, Vector3 rotation, Vector3 scale)
    {
      TemplatePositionOffset = position;
      TemplateRotationOffset = rotation;
      TemplateScaleOffset    = scale;

      Utils.Log($"[WaterfallEffect] Applying template offsets from FN2 {position}, {rotation}, {scale}", LogType.Effects);


      for (int i = 0; i < effectTransforms.Count; i++)
      {
        effectTransforms[i].localPosition = TemplatePositionOffset;
        effectTransforms[i].localScale    = Vector3.Scale(baseScales[i], TemplateScaleOffset);

        if (TemplateRotationOffset == Vector3.zero)
        {
          effectTransforms[i].localRotation = Quaternion.identity;
        }
        else
        {
          effectTransforms[i].localEulerAngles = TemplateRotationOffset;
        }
      }
    }

    public List<Transform> GetModelTransforms() => model.modelTransforms;

    public void Update()
    {
      if (effectVisible)
      {
        model.Update();
        for (int i = 0; i < fxModifiers.Count; i++)
        {
          fxModifiers[i].Apply(parentModule.GetControllerValue(fxModifiers[i].controllerName));
        }

        for (int i = 0; i < floatIntegrators.Count; i++)
        {
          floatIntegrators[i].Update();
        }

        for (int i = 0; i < colorIntegrators.Count; i++)
        {
          colorIntegrators[i].Update();
        }

        for (int i = 0; i < positionIntegrators.Count; i++)
        {
          positionIntegrators[i].Update();
        }

        for (int i = 0; i < scaleIntegrators.Count; i++)
        {
          scaleIntegrators[i].Update();
        }

        for (int i = 0; i < rotationIntegrators.Count; i++)
        {
          rotationIntegrators[i].Update();
        }

        for (int i = 0; i < lightFloatIntegrators.Count; i++)
        {
          lightFloatIntegrators[i].Update();
        }

        for (int i = 0; i < lightColorIntegrators.Count; i++)
        {
          lightColorIntegrators[i].Update();
        }


        int transparentQueueBase = 3000;

        int   queueDepth   = 750;
        float sortedDepth  = 1000f;
        int   distortQueue = transparentQueueBase + 2;

        var c = FlightCamera.fetch.cameras[0].transform;
        for (int i = 0; i < effectRendererMaterials.Count; i++)
        {
          float camDistBounds    = Vector3.Dot(effectRenderers[i].bounds.center      - c.position, c.forward);
          float camDistTransform = Vector3.Dot(effectRenderers[i].transform.position - c.position, c.forward);

          int qDelta = queueDepth - (int)Mathf.Clamp(Mathf.Min(camDistBounds, camDistTransform) / sortedDepth * queueDepth, 0, queueDepth);
          if (effectRendererMaterials[i].HasProperty("_Strength"))
            qDelta = distortQueue;
          if (effectRendererMaterials[i].HasProperty("_Intensity"))
            qDelta += 1;
          effectRendererMaterials[i].renderQueue = transparentQueueBase + qDelta;
        }
      }
    }

    public void SetHDR(bool isHDR)
    {
      float destMode = Settings.EnableLegacyBlendModes ? 6 : 1;

      foreach (var mat in effectRendererMaterials)
      {
        if (mat.HasProperty("_DestMode"))
        {
          mat.SetFloat("_DestMode", isHDR ? 1 : destMode);
          mat.SetFloat("_ClipBrightness", isHDR ? 50: 1);
        }
      }
    }

    public void RemoveModifier(EffectModifier mod)
    {
      fxModifiers.Remove(mod);
      if (mod is EffectFloatModifier f) RemoveModifier(f, floatIntegrators);
      else if (mod is EffectColorModifier c) RemoveModifier(c, colorIntegrators);
      else if (mod is EffectPositionModifier p) RemoveModifier(p, positionIntegrators);
      else if (mod is EffectRotationModifier r) RemoveModifier(r, rotationIntegrators);
      else if (mod is EffectScaleModifier s) RemoveModifier(s, scaleIntegrators);
      else if (mod is EffectLightFloatModifier light) RemoveModifier(light, lightFloatIntegrators);
      else if (mod is EffectLightColorModifier lightColor) RemoveModifier(lightColor, lightColorIntegrators);
    }

    public void ModifierParameterChange(EffectModifier mod)
    {
      RemoveModifier(mod);
      AddModifier(mod);
    }

    public void AddModifier(EffectModifier mod)
    {
      mod.Init(this);
      fxModifiers.Add(mod);
      if (mod is EffectFloatModifier e) ParseFloatModifier(e);
      else if (mod is EffectColorModifier c) ParseColorModifier(c);
      else if (mod is EffectPositionModifier p) ParsePositionModifier(p);
      else if (mod is EffectRotationModifier r) ParseRotationModifier(r);
      else if (mod is EffectScaleModifier s) ParseScaleModifier(s);
      else if (mod is EffectLightFloatModifier light) ParseLightFloatModifier(light);
      else if (mod is EffectLightColorModifier lightColor) ParseLightColorModifier(lightColor);
    }

    public void MoveModifierFromTo(int oldIndex, int newIndex)
    {
      oldIndex = Mathf.Clamp(oldIndex, 0, fxModifiers.Count - 1);
      newIndex = Mathf.Clamp(newIndex, 0, fxModifiers.Count - 1);

      var item = fxModifiers[oldIndex];
      fxModifiers.RemoveAt(oldIndex);
      fxModifiers.Insert(newIndex, item);

      InitializeIntegrators();
    }

    public void MoveModifierUp(int index) => MoveModifierFromTo(index, index - 1);
    public void MoveModifierDown(int index) => MoveModifierFromTo(index, index + 1);

    public void SetEnabled(bool state)
    {
      for (int i = 0; i < effectTransforms.Count; i++)
        effectTransforms[i].localScale = state ? Vector3.Scale(baseScales[i], TemplateScaleOffset) : Vector3.one * 0.00001f;
      effectVisible = state;
    }

    private void ParseFloatModifier(EffectModifier fxMod)
    {
      try
      {
        var floatMod = (EffectFloatModifier)fxMod;
        if (floatMod != null)
        {
          bool                  needsNewIntegrator = true;
          EffectFloatIntegrator targetIntegrator   = null;

          foreach (var floatInt in floatIntegrators)
          {
            // If already exists as a handled modifier, don't touch me
            if (floatInt.handledModifiers.Contains(floatMod))
              return;

            // if there's already an integrator that has the transform name and float name, don't need to add
            if (floatInt.floatName == floatMod.floatName && floatInt.transformName == floatMod.transformName)
            {
              targetIntegrator   = floatInt;
              needsNewIntegrator = false;
            }
          }

          if (needsNewIntegrator && floatMod.floatName != "")
          {
            var newIntegrator = new EffectFloatIntegrator(this, floatMod);
            floatIntegrators.Add(newIntegrator);
          }
          else if (!needsNewIntegrator && floatMod.floatName != "")
          {
            if (targetIntegrator != null)
            {
              targetIntegrator.AddModifier(floatMod);
            }
          }
        }
      }
      catch (InvalidCastException e) { }
    }

    private void ParseColorModifier(EffectModifier fxMod)
    {
      try
      {
        var colorMod = (EffectColorModifier)fxMod;
        if (colorMod != null)
        {
          bool                  needsNewIntegrator = true;
          EffectColorIntegrator targetIntegrator   = null;

          foreach (var integrator in colorIntegrators)
          {
            // If already exists as a handled modifier, don't touch me
            if (integrator.handledModifiers.Contains(colorMod))
              return;

            // if there's already an integrator that has the transform name and float name, don't need to add
            if (integrator.colorName == colorMod.colorName && integrator.transformName == colorMod.transformName)
            {
              targetIntegrator   = integrator;
              needsNewIntegrator = false;
            }
          }

          if (needsNewIntegrator && colorMod.colorName != "")
          {
            var newIntegrator = new EffectColorIntegrator(this, colorMod);
            colorIntegrators.Add(newIntegrator);
          }
          else if (!needsNewIntegrator && colorMod.colorName != "")
          {
            if (targetIntegrator != null)
            {
              targetIntegrator.AddModifier(colorMod);
            }
          }
        }
      }
      catch (InvalidCastException e) { }
    }

    private void ParsePositionModifier(EffectModifier fxMod)
    {
      try
      {
        var posMod = (EffectPositionModifier)fxMod;
        if (posMod != null)
        {
          bool                     needsNewIntegrator = true;
          EffectPositionIntegrator targetIntegrator   = null;

          foreach (var integrator in positionIntegrators)
          {
            // If already exists as a handled modifier, don't touch me
            if (integrator.handledModifiers.Contains(posMod))
              return;

            // if there's already an integrator that has the transform name and float name, don't need to add
            if (integrator.transformName == posMod.transformName)
            {
              targetIntegrator   = integrator;
              needsNewIntegrator = false;
            }
          }

          if (needsNewIntegrator)
          {
            var newIntegrator = new EffectPositionIntegrator(this, posMod);
            positionIntegrators.Add(newIntegrator);
          }
          else if (!needsNewIntegrator)
          {
            if (targetIntegrator != null)
            {
              targetIntegrator.AddModifier(posMod);
            }
          }
        }
      }
      catch (InvalidCastException e) { }
    }

    private void ParseScaleModifier(EffectModifier fxMod)
    {
      try
      {
        var scaleMod = (EffectScaleModifier)fxMod;
        if (scaleMod != null)
        {
          bool                  needsNewIntegrator = true;
          EffectScaleIntegrator targetIntegrator   = null;

          foreach (var integrator in scaleIntegrators)
          {
            // If already exists as a handled modifier, don't touch me
            if (integrator.handledModifiers.Contains(scaleMod))
              return;

            // if there's already an integrator that has the transform name and float name, don't need to add
            if (integrator.transformName == scaleMod.transformName)
            {
              targetIntegrator   = integrator;
              needsNewIntegrator = false;
            }
          }

          if (needsNewIntegrator)
          {
            var newIntegrator = new EffectScaleIntegrator(this, scaleMod);
            scaleIntegrators.Add(newIntegrator);
          }
          else if (!needsNewIntegrator)
          {
            if (targetIntegrator != null)
            {
              targetIntegrator.AddModifier(scaleMod);
            }
          }
        }
      }
      catch (InvalidCastException e) { }
    }

    private void ParseRotationModifier(EffectModifier fxMod)
    {
      try
      {
        var rotMod = (EffectRotationModifier)fxMod;
        if (rotMod != null)
        {
          bool                     needsNewIntegrator = true;
          EffectRotationIntegrator targetIntegrator   = null;

          foreach (var integrator in rotationIntegrators)
          {
            // If already exists as a handled modifier, don't touch me
            if (integrator.handledModifiers.Contains(rotMod))
              return;

            // if there's already an integrator that has the transform name and float name, don't need to add
            if (integrator.transformName == rotMod.transformName)
            {
              targetIntegrator   = integrator;
              needsNewIntegrator = false;
            }
          }

          if (needsNewIntegrator)
          {
            var newIntegrator = new EffectRotationIntegrator(this, rotMod);
            rotationIntegrators.Add(newIntegrator);
          }
          else if (!needsNewIntegrator)
          {
            if (targetIntegrator != null)
            {
              targetIntegrator.AddModifier(rotMod);
            }
          }
        }
      }
      catch (InvalidCastException e) { }
    }

    private void ParseLightFloatModifier(EffectModifier fxMod)
    {
      try
      {
        var floatMod = (EffectLightFloatModifier)fxMod;
        if (floatMod != null)
        {
          bool                       needsNewIntegrator = true;
          EffectLightFloatIntegrator targetIntegrator   = null;

          foreach (var floatInt in lightFloatIntegrators)
          {
            // If already exists as a handled modifier, don't touch me
            if (floatInt.handledModifiers.Contains(floatMod))
              return;

            // if there's already an integrator that has the transform name and float name, don't need to add
            if (floatInt.floatName == floatMod.floatName && floatInt.transformName == floatMod.transformName)
            {
              targetIntegrator   = floatInt;
              needsNewIntegrator = false;
            }
          }

          if (needsNewIntegrator && floatMod.floatName != "")
          {
            var newIntegrator = new EffectLightFloatIntegrator(this, floatMod);
            lightFloatIntegrators.Add(newIntegrator);
          }
          else if (!needsNewIntegrator && floatMod.floatName != "")
          {
            if (targetIntegrator != null)
            {
              targetIntegrator.AddModifier(floatMod);
            }
          }
        }
      }
      catch (InvalidCastException e) { }
    }

    private void ParseLightColorModifier(EffectModifier fxMod)
    {
      try
      {
        var colorMod = (EffectLightColorModifier)fxMod;
        if (colorMod != null)
        {
          bool                       needsNewIntegrator = true;
          EffectLightColorIntegrator targetIntegrator   = null;

          foreach (var integrator in lightColorIntegrators)
          {
            // If already exists as a handled modifier, don't touch me
            if (integrator.handledModifiers.Contains(colorMod))
              return;

            // if there's already an integrator that has the transform name and float name, don't need to add
            if (integrator.colorName == colorMod.colorName && integrator.transformName == colorMod.transformName)
            {
              targetIntegrator   = integrator;
              needsNewIntegrator = false;
            }
          }

          if (needsNewIntegrator && colorMod.colorName != "")
          {
            var newIntegrator = new EffectLightColorIntegrator(this, colorMod);
            lightColorIntegrators.Add(newIntegrator);
          }
          else if (!needsNewIntegrator && colorMod.colorName != "")
          {
            if (targetIntegrator != null)
            {
              targetIntegrator.AddModifier(colorMod);
            }
          }
        }
      }
      catch (InvalidCastException e) { }
    }

    private void RemoveModifier<T>(EffectModifier fxMod, List<T> integrators) where T : EffectIntegrator
    {
      if (fxMod == null || integrators == null) return;
      if (integrators.FirstOrDefault(x => x.handledModifiers.Contains(fxMod)) is T integrator)
        integrator.RemoveModifier(fxMod);
    }
  }
}