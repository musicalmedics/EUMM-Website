CREATE TABLE [dbo].[Groups]
(
	[ID] INT NOT NULL PRIMARY KEY,
	[Name] VARCHAR NOT NULL,
	[Description] VARCHAR,
	[CreatorUUN] NCHAR (8) NOT NULL,
	[Active] BIT DEFAULT '1',
	[IsPresent] BIT DEFAULT '1',
	CONSTRAINT [Groups_FK_Groups_Members] FOREIGN KEY ([CreatorUUN]) REFERENCES [dbo].[Members] ([UUN])
);
