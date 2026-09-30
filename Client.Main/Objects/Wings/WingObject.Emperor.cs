namespace Client.Main.Objects.Wings
{
    public partial class WingObject
    {
        private EmperorCapeCloth _emperorCapeCloth;


        /// <summary>
        /// True only while the classic Cape of Emperor
        /// (item group 12, index 40) is actually equipped.
        /// </summary>
        internal bool IsEmperorCapeClothActive =>
            _isEmperorCape &&
            ItemIndex == EmperorCapeItemIndex &&
            Model != null;


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
    }
}