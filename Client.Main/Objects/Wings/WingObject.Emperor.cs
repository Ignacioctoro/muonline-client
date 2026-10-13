namespace Client.Main.Objects.Wings
{
    public partial class WingObject
    {
        private EmperorCapeCloth _emperorCapeCloth;
        private EmperorCapeCloth _overruleCapeCloth;
        private EmperorCapeCloth _lordCapeCloth;
        private EmperorCapeCloth _fighterCapeCloth;


        /// <summary>
        /// True only while the classic Cape of Emperor
        /// (item group 12, index 40) is actually equipped.
        /// </summary>
        internal bool IsEmperorCapeClothActive =>
            _isEmperorCape &&
            ItemIndex == EmperorCapeItemIndex &&
            Model != null;

        internal bool IsOverruleCapeClothActive =>
            _isOverruleCape &&
            ItemIndex == OverruleCapeItemIndex &&
            Model != null;

        // S2 cloth is simulated independently from the normal BMD renderer.
        // It uses player bone 19, while the rigid S2 BMD follows the
        // original default wing attachment on bone 47.
        internal bool IsLordCapeClothActive =>
            ItemIndex == LordCapeItemIndex && Model != null;

        internal bool IsFighterCapeClothActive =>
            ItemIndex == FighterCapeItemIndex && Model != null;


        /// <summary>
        /// WingObject is normally attached to PlayerObject.
        ///
        /// We attach the cloth object as a child of WingObject only
        /// for lifecycle purposes. EmperorCapeCloth does NOT use the
        /// WingObject transform for its geometry.
        ///
        /// It reads the PLAYER bone matrices directly, matching the
        /// original MU CPhysicsCloth implementation.
        /// </summary>
        protected override void OnParentChanged(
            WorldObject current,
            WorldObject previous)
        {
            base.OnParentChanged(
                current,
                previous);


            if (current != null)
            {
                EnsureEmperorCapeCloth();

                if (ItemIndex == OverruleCapeItemIndex)
                    EnsureOverruleCapeCloth();
                else if (ItemIndex == LordCapeItemIndex)
                    EnsureLordCapeCloth();
                else if (ItemIndex == FighterCapeItemIndex)
                    EnsureFighterCapeCloth();
            }
        }


        private void EnsureEmperorCapeCloth()
        {
            if (_emperorCapeCloth == null)
            {
                _emperorCapeCloth =
                    new EmperorCapeCloth(
                        this);
            }


            if (!Children.Contains(
                    _emperorCapeCloth))
            {
                Children.Add(
                    _emperorCapeCloth);
            }
        }
        // RF Overrule uses the same CPhysicsCloth integrator as DL,
        // but it is only allocated when this actual item is equipped.
        private void EnsureOverruleCapeCloth()
        {
            if (Parent == null)
                return;

            if (_overruleCapeCloth == null)
                _overruleCapeCloth = new EmperorCapeCloth(this, overruleCape: true);

            if (!Children.Contains(_overruleCapeCloth))
                Children.Add(_overruleCapeCloth);
        }

        private void EnsureLordCapeCloth()
        {
            if (Parent == null)
                return;

            if (_lordCapeCloth == null)
                _lordCapeCloth = new EmperorCapeCloth(this, lordCape: true);

            if (!Children.Contains(_lordCapeCloth))
                Children.Add(_lordCapeCloth);
        }

        private void EnsureFighterCapeCloth()
        {
            if (Parent == null)
                return;

            if (_fighterCapeCloth == null)
                _fighterCapeCloth = new EmperorCapeCloth(this, fighterCape: true);

            if (!Children.Contains(_fighterCapeCloth))
                Children.Add(_fighterCapeCloth);
        }
    }
}