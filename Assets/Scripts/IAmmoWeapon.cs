using System;

public interface IAmmoWeapon
{
    int CurrentAmmo { get; }
    int MaxAmmo { get; }
    string AmmoDisplayName { get; }
    event Action<IAmmoWeapon> AmmoChanged;
}
