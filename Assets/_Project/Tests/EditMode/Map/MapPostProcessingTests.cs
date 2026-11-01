using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FateBastion.Editor.UI;

namespace FateBastion.Tests.EditMode.Map
{
    /// <summary>Map_Level1 carries the match post-processing, so Game.unity glows like the sandbox (C-09, C-17).</summary>
    public class MapPostProcessingTests
    {
        [Test]
        public void MapLevel1_HasGlobalVolumeWithMatchProfile()
        {
            var map = AssetDatabase.LoadAssetAtPath<GameObject>(PostProcessingInstaller.MapPath);
            Assert.IsNotNull(map);
            Transform holder = map.transform.Find("Visuals/" + PostProcessingInstaller.VolumeName);
            Assert.IsNotNull(holder, "Thiếu Visuals/PostProcessVolume: chạy Tools > Map > Add Post Processing to Map_Level1.");

            Type volumeType = Type.GetType(PostProcessingInstaller.VolumeTypeName);
            Assert.IsNotNull(volumeType);
            Component volume = holder.GetComponent(volumeType);
            Assert.IsNotNull(volume);
            var data = new SerializedObject(volume);
            Assert.IsTrue(data.FindProperty("m_IsGlobal").boolValue, "Volume phải là Global.");
            Assert.AreEqual(PostProcessingInstaller.ProfilePath,
                AssetDatabase.GetAssetPath(data.FindProperty("sharedProfile").objectReferenceValue));
        }
    }
}
