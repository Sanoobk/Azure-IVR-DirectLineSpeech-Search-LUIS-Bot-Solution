USE [IVRBotdB]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Bikes] (
    [BikeId]         NVARCHAR (450)    NOT NULL,
    [BaseRate]       FLOAT (53)        NULL,
    [Category]       NVARCHAR (MAX)    NULL,
    [Description]    NVARCHAR (MAX)    NULL,
    [Description_fr] NVARCHAR (MAX)    NULL,
    [BikeName]       NVARCHAR (MAX)    NULL,
    [Tags]           NVARCHAR (MAX)    NULL,
    [IsDeleted]      BIT               NOT NULL,
    [Color]          NVARCHAR (MAX)    NULL,
    [Electric]       BIT               NULL,
    [Rating]         INT               NULL,
    [Date]           DATETIME          NULL,
    [Location]       [sys].[geography] NULL
);


GO
ALTER TABLE [dbo].[Bikes] ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);

INSERT INTO [dbo].[Bikes] ([BikeId], [BaseRate], [Category], [Description], [Description_fr], [BikeName], [Tags], [IsDeleted], [Color], [Electric], [Rating], [Date], [Location]) VALUES (N'B001', 100, N'Mountain', N'Mountain bike', NULL, N'Mountain King', N'["Sporty","Tough"]', 0, N'Red', 0, 4, N'2018-12-12 00:00:00', NULL)
INSERT INTO [dbo].[Bikes] ([BikeId], [BaseRate], [Category], [Description], [Description_fr], [BikeName], [Tags], [IsDeleted], [Color], [Electric], [Rating], [Date], [Location]) VALUES (N'B002', 150, N'Mountain', N'Mountain bike', NULL, N'Mountain King', N'["Sporty","Tough"]', 0, N'Blue', 0, 4, N'2017-12-12 00:00:00', NULL)
INSERT INTO [dbo].[Bikes] ([BikeId], [BaseRate], [Category], [Description], [Description_fr], [BikeName], [Tags], [IsDeleted], [Color], [Electric], [Rating], [Date], [Location]) VALUES (N'B003', 350, N'Roadster', N'Road compatible bike', NULL, N'Road Tiger', N'["Simple","Elegant"]', 0, N'Green', 1, 5, N'2019-10-10 00:00:00', NULL)
INSERT INTO [dbo].[Bikes] ([BikeId], [BaseRate], [Category], [Description], [Description_fr], [BikeName], [Tags], [IsDeleted], [Color], [Electric], [Rating], [Date], [Location]) VALUES (N'B004', 500, N'Roadster', N'Road compatible bike with high electric efficiency', NULL, N'Road Tiger Plus', N'["Simple","Elegant","Power"]', 0, N'Blue', 1, 5, N'2019-10-06 00:00:00', NULL)


