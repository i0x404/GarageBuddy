﻿namespace GarageBuddy.Services.Data.Contracts
{
    using Models.Vehicle.GearboxType;

    public interface IGearboxTypeService
    {
        public Task<ICollection<GearBoxTypeServiceModel>> GetAllAsync();

        public Task<ICollection<GearBoxTypeSelectServiceModel>> GetAllSelectAsync();
    }
}