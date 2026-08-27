This project provides instructions to set up and start the application, including necessary dependencies.

## Prerequisites

Before starting the application, make sure you have the following installed:

- **Node.js version 22** (using Node Version Manager)
- **NVM (Node Version Manager)**

## Setup Instructions

Follow these steps to set up and start the application:

### 1. Install NVM

If you haven't installed NVM yet, follow the instructions for your operating system:

- [Install NVM for Windows](https://github.com/coreybutler/nvm-windows)
- [Install NVM for macOS/Linux](https://github.com/nvm-sh/nvm)

### 2. Install Node.js Version 22

After installing NVM, install Node.js version 22 by running:

nvm install 22
3. Use Node.js Version 22
Set the current version to Node.js 22:

nvm use 22
4. Configure JFrog Artifactory in .npmrc
Make sure your .npmrc file (located at C:\Users\YourName for Windows or ~/.npmrc for macOS/Linux) includes the following configuration:

registry=https://kdi.jfrog.io/artifactory/api/npm/npm/
always-auth=true
//kdi.jfrog.io/artifactory/api/npm/npm/:_authToken={your_token}
Replace {your_token} with the token generated from JFrog.

How to Get the JFrog Token
Log in to JFrog Artifactory using SSO.
Navigate to Set Me Up > npm > Generate Token.
Copy the generated token and update your .npmrc file.
5. Clone the Repository
Clone the project repository to your local machine:

git clone https://your-repo-url.git
Replace https://your-repo-url.git with the actual repository URL.

6. Install Dependencies
Navigate to the project directory and install the dependencies:

cd your-project-directory
npm install
Replace your-project-directory with the name of the directory where the project was cloned.

7. Start the Application
To start the application, run:

ng serve
The application should now be running. Open a web browser and go to http://localhost:4200 to see the app.

Troubleshooting
If you encounter any issues, try the following:

Verify Node.js Version: Make sure you have Node.js version 22 active:

node -v
Check JFrog Authentication: Ensure your .npmrc configuration is correct and the token is valid.