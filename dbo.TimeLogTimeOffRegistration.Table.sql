USE [DataWarehouse]
GO
/****** Object:  Table [dbo].[TimeLogTimeOffRegistration]    Script Date: 09-12-2015 16:52:52 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TimeLogTimeOffRegistration](
	[ID] [int] NOT NULL,
	[EmployeeID] [int] NULL,
	[EmployeeInitials] [nvarchar](150) NULL,
	[EmployeeFirstName] [nvarchar](150) NULL,
	[EmployeeLastName] [nvarchar](150) NULL,
	[TimeOffCode] [nvarchar](150) NULL,
	[TimeOffName] [nvarchar](150) NULL,
	[ProjectID] [int] NULL,
	[Date] [date] NULL,
	[RegHours] [decimal](18, 2) NULL,
 CONSTRAINT [PK_TimeOffRegistration] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
