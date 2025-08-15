# Kit CLI Profile Management

Kit CLI now supports multiple configuration profiles, allowing you to manage different Kit accounts or API keys easily.

## Profile Commands

### Setting Configuration with Profiles

When you set configuration with a profile, it automatically becomes the default if it's the first profile:

```bash
# First profile - automatically becomes default
kit config set --api-key YOUR_KEY --profile personal

# Adding another profile - prompts to set as default
kit config set --api-key WORK_KEY --profile work
# > Set 'work' as default profile? (current: personal) [y/N]:

# Force setting as default
kit config set --api-key NEW_KEY --profile staging --set-default
```

### Listing Profiles

View all configured profiles and see which is currently active:

```bash
kit config profiles
# Current default profile: personal
# 
# Available profiles:
#   * personal
#      API Key: kit_...1234
#     work
#      API Key: kit_...5678
```

### Switching Default Profile

Change the default profile used for commands:

```bash
kit config profile work
# ✓ Switched to profile: work
```

### Using Profiles with Commands

You can override the default profile for any command using the `--profile` flag:

```bash
# Use work profile for this command only
kit subscriber list --profile work

# Test connection with a specific profile
kit config test --profile staging

# View config for a specific profile
kit config get --profile personal
```

### Verbose Mode

When using verbose mode, the current profile is displayed:

```bash
kit subscriber list --verbose
# Using profile: work
# [subscriber list output...]
```

## Environment Variables

You can also set configuration via environment variables (overrides all profiles):

```bash
export KIT_API_KEY=your_api_key
kit subscriber list  # Uses env var instead of profile
```

## Security Notes

- API keys are stored in `~/.kit/config.json` with secure file permissions (600 on Unix)
- API keys are masked when displayed (showing only first 4 and last 4 characters)
- Never commit your config file to version control

## Common Workflows

### Multiple Accounts
```bash
# Personal blog
kit config set --api-key PERSONAL_KEY --profile personal
kit subscriber list --profile personal

# Work account
kit config set --api-key WORK_KEY --profile work
kit subscriber list --profile work

# Set work as default
kit config profile work
kit subscriber list  # Now uses work profile by default
```

### Testing Different Environments
```bash
# Production
kit config set --api-key PROD_KEY --profile production

# Staging
kit config set --api-key STAGING_KEY --profile staging

# Quick test on staging
kit config test --profile staging

# Export from production
kit subscriber list --profile production --export prod-subs.csv
```