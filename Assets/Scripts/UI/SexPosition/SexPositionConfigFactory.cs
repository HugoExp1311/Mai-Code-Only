using System;

namespace UI.SexPosition
{
    /// <summary>
    /// Factory for creating sex position configuration instances
    /// </summary>
    public static class SexPositionConfigFactory
    {
        /// <summary>
        /// Creates a position configuration based on the specified type
        /// </summary>
        public static SexPositionConfig Create(SexPositionConfigType type)
        {
            switch (type)
            {
                case SexPositionConfigType.Missionary:
                    return new MissionaryPositionConfig();

                case SexPositionConfigType.RoleplayPussy:
                    return new RoleplayPussyPositionConfig();

                case SexPositionConfigType.RoleplayButthole:
                    return new RoleplayButtholePositionConfig();

                case SexPositionConfigType.RoleplayBlowjob:
                    return new RoleplayBlowjobPositionConfig();

                case SexPositionConfigType.RoleplayPaizuri:
                    return new RoleplayPaizuriPositionConfig();

                case SexPositionConfigType.Doggy:
                    throw new NotImplementedException("Doggy position not yet implemented");

                case SexPositionConfigType.Cowgirl:
                    return new CowgirlPositionConfig();

                case SexPositionConfigType.Standing:
                    throw new NotImplementedException("Standing position not yet implemented");

                default:
                    // Default to missionary if unknown type
                    return new MissionaryPositionConfig();
            }
        }
    }
}
