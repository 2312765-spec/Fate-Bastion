namespace FateBastion.Editor.Heroes
{
    /// <summary>Import profile of a third-party FBX, chosen from the folder it sits in (C-07).</summary>
    public enum ThirdPartyModelKind
    {
        /// <summary>Not under a known folder: the postprocessor leaves the importer alone.</summary>
        None,

        /// <summary>Rigged human (heroes, Summoner): Humanoid avatar so every clip fits every human model.</summary>
        HumanoidCharacter,

        /// <summary>Mixamo clip downloaded "Without Skin": Humanoid, animation only, no materials.</summary>
        HumanoidAnimation,

        /// <summary>Non-human rig (monsters): Generic, keeps the clips shipped inside the FBX.</summary>
        GenericCreature,

        /// <summary>Static mesh (map props, accessories such as Long Vương's horns): no rig, no animation.</summary>
        StaticProp
    }
}
