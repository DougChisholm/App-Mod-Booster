# Chat UI Module

This folder contains the chat user interface integration with Azure OpenAI.

## Features

- Natural language queries for expense management
- Function calling for real database operations
- Formatted list displays in chat bubbles
- Context-aware responses using conversation history

## How It Works

1. **User Input**: User types a natural language query
2. **OpenAI Processing**: Query sent to Azure OpenAI GPT-4o
3. **Function Calling**: AI determines which database operations to perform
4. **API Execution**: Functions call the backend APIs
5. **Response Formatting**: Results formatted nicely for display

## Available Functions

| Function | Description |
|----------|-------------|
| `get_expenses` | List expenses with filters |
| `get_pending_expenses` | Get expenses awaiting approval |
| `get_expense_by_id` | Get specific expense details |
| `create_expense` | Create a new expense |
| `submit_expense` | Submit expense for approval |
| `approve_expense` | Approve a pending expense |
| `reject_expense` | Reject a pending expense |
| `get_categories` | List expense categories |
| `get_users` | List system users |
| `get_expense_summary` | Summary by status |
| `get_expenses_by_category` | Summary by category |

## Example Queries

- "Show me all expenses"
- "What expenses are pending approval?"
- "Create a travel expense for £50 dated today"
- "Approve expense number 1"
- "Show expense summary by status"

## GenAI Disabled Mode

If Azure OpenAI is not configured, the chat UI displays a helpful message explaining:
- GenAI services are not deployed
- Instructions to use `deploy-with-chat.sh`
- What features would be available

## Response Formatting

The chat UI handles:
- **Bold text**: `**text**` → `<strong>text</strong>`
- **Numbered lists**: Lines starting with `1.` → `<ol><li>`
- **Bullet lists**: Lines starting with `-` or `*` → `<ul><li>`
- **Line breaks**: `\n` → `<br>`
- **HTML escaping**: Prevents XSS attacks

## Configuration

Settings are read from App Service configuration:

```json
{
  "OpenAI": {
    "Endpoint": "https://oai-xxx.openai.azure.com/",
    "DeploymentName": "gpt-4o"
  },
  "ManagedIdentityClientId": "guid-of-managed-identity"
}
```
