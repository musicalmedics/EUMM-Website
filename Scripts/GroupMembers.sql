CREATE TABLE [dbo].[GroupMembers] (
    [Group]   INT       NOT NULL,
    [Member]  NCHAR (8) NOT NULL,
    CONSTRAINT [GroupMembers_FK_GroupMembers_Group] FOREIGN KEY ([Group]) REFERENCES [dbo].[Groups] ([ID]),
    CONSTRAINT [GroupMembers_FK_GroupMembers_Member] FOREIGN KEY ([Member]) REFERENCES [dbo].[Members] ([UUN]),
	PRIMARY KEY ([Group], [Member])
);
